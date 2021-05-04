using System;
using System.Linq;
using System.Threading.Tasks;
using Administrator.Database;
using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using Disqord.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Administrator.Services
{
    public sealed class DatabaseCleanupService : DiscordClientService
    {
        private readonly IServiceProvider _services;
        
        public DatabaseCleanupService(ILogger<DatabaseCleanupService> logger, DiscordBotBase bot)
            : base(logger, bot)
        {
            _services = bot.Services;
        }

        protected override async ValueTask OnRoleDeleted(RoleDeletedEventArgs e)
        {
            using var scope = _services.CreateScope();
            await using var ctx = scope.ServiceProvider.GetRequiredService<AdminDbContext>();

            if (await ctx.SpecialRoles.FirstOrDefaultAsync(x => x.Id == e.RoleId) is { } specialRole)
            {
                Logger.LogDebug("Removing special role {RoleId} because the role attached to it was deleted.",
                    specialRole.Id.RawValue);
                
                ctx.SpecialRoles.Remove(specialRole);
            }

            await ctx.SaveChangesAsync();
        }

        protected override async ValueTask OnGuildEmojisUpdated(GuildEmojisUpdatedEventArgs e)
        {
            using var scope = _services.CreateScope();
            await using var ctx = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
            
            var removedIds = e.OldEmojis.Keys.Except(e.NewEmojis.Keys);
            var modifiedEmojis = e.NewEmojis.Values.Where(x =>
                e.OldEmojis.TryGetValue(x.Id, out var emoji) && !emoji.Name.Equals(x.Name));
            
            foreach (var removedId in removedIds)
            {
                if (await ctx.FindAsync<BigEmoji>(removedId) is { } bigEmoji)
                {
                    Logger.LogDebug("Removing big emoji {EmojiId} because the emoji attached to it was deleted.",
                        removedId.RawValue);
                    
                    ctx.BigEmojis.Remove(bigEmoji);
                }

                var specialEmojis = await ctx.SpecialEmojis.ToListAsync();
                if (specialEmojis.FirstOrDefault(x => x.Emoji is LocalCustomEmoji em && em.Id == removedId) is { } specialEmoji)
                {
                    Logger.LogDebug("Removing special emoji {EmojiId} because the emoji attached to it was deleted.",
                        removedId.RawValue);
                    
                    ctx.SpecialEmojis.Remove(specialEmoji);
                }
            }

            foreach (var modifiedEmoji in modifiedEmojis)
            {
                if (await ctx.FindAsync<BigEmoji>(modifiedEmoji.Id) is { } bigEmoji)
                {
                    Logger.LogDebug("Removing big emoji {EmojiId} because the emoji attached to it had its name updated.",
                        modifiedEmoji.Id.RawValue);
                    
                    ctx.BigEmojis.Remove(bigEmoji);
                }
                
                var specialEmojis = await ctx.SpecialEmojis.ToListAsync();
                if (specialEmojis.FirstOrDefault(x => x.Emoji is LocalCustomEmoji em && em.Id == modifiedEmoji.Id) is { } specialEmoji)
                {
                    Logger.LogDebug("Updating special emoji {EmojiId} because the emoji attached to it had its name updated.",
                        modifiedEmoji.Id.RawValue);
                    
                    specialEmoji.Emoji = modifiedEmoji;
                    ctx.SpecialEmojis.Update(specialEmoji);
                }
            }

            await ctx.SaveChangesAsync();
        }

        protected override async ValueTask OnGuildUpdated(GuildUpdatedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.NewGuild.Name) || string.IsNullOrWhiteSpace(e.OldGuild.Name))
                return; // Jesus christ Discord please
            
            using var scope = _services.CreateScope();
            await using var ctx = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
            var guild = await ctx.GetOrCreateGuildAsync(e.NewGuild);

            if (!guild.Name.Equals(e.NewGuild.Name))
            {
                Logger.LogDebug("Updated guild {GuildId} because the guild attached to it had its name updated.",
                    e.GuildId.RawValue);
                
                guild.Name = e.NewGuild.Name;
                ctx.Guilds.Update(guild);
            }

            await ctx.SaveChangesAsync();
        }
    }
}