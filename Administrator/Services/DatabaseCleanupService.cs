using System;
using System.Linq;
using System.Threading.Tasks;
using Administrator.Database;
using Disqord.Events;
using Microsoft.EntityFrameworkCore;

namespace Administrator.Services
{
    public sealed class DatabaseCleanupService : Service,
        IHandler<ChannelDeletedEventArgs>,
        IHandler<RoleDeletedEventArgs>,
        IHandler<LeftGuildEventArgs>
    { 
        public DatabaseCleanupService(IServiceProvider provider) 
            : base(provider)
        { }

        public async Task HandleAsync(ChannelDeletedEventArgs args)
        {
            using var ctx = new AdminDatabaseContext(_provider);

            foreach (var entry in ctx.Starboard.Where(x => x.ChannelId == args.Channel.Id || x.EntryChannelId == args.Channel.Id))
            {
                ctx.Starboard.Remove(entry);
                await ctx.SaveChangesAsync();

                if (entry.EntryChannelId != args.Channel.Id)
                    await args.Client.DeleteMessageAsync(entry.EntryChannelId, entry.EntryMessageId);
            }

            var channels = await ctx.LoggingChannels.Where(x => x.Id == args.Channel.Id)
                .ToListAsync();

            if (channels.Count > 0)
            {
                ctx.LoggingChannels.RemoveRange(channels);
                await ctx.SaveChangesAsync();
            }
        }

        public async Task HandleAsync(RoleDeletedEventArgs args)
        {
            using var ctx = new AdminDatabaseContext(_provider);

            var roles = await ctx.SpecialRoles.Where(x => x.Id == args.Role.Id)
                .ToListAsync();

            if (roles.Count > 0)
            {
                ctx.SpecialRoles.RemoveRange(roles);
                await ctx.SaveChangesAsync();
            }
        }

        public async Task HandleAsync(LeftGuildEventArgs args)
        {
            using var ctx = new AdminDatabaseContext(_provider);
            var changes = false;

            if (await ctx.Guilds.FindAsync(args.Guild.Id) is { } guild)
            {
                ctx.Guilds.Remove(guild);
                changes = true;
            }

            var channels = await ctx.LoggingChannels.Where(x => x.GuildId == args.Guild.Id).ToListAsync();
            if (channels.Count > 0)
            {
                ctx.LoggingChannels.RemoveRange(channels); 
                changes = true;
            }

            var roles = await ctx.SpecialRoles.Where(x => x.GuildId == args.Guild.Id).ToListAsync();
            if (roles.Count > 0)
            {
                ctx.SpecialRoles.RemoveRange(roles);
                changes = true;
            }

            var reactionRoles = await ctx.ReactionRoles.Where(x => x.GuildId == args.Guild.Id).ToListAsync();
            if (reactionRoles.Count > 0)
            {
                ctx.ReactionRoles.RemoveRange(reactionRoles);
                changes = true;
            }

            var users = await ctx.GuildUsers.Where(x => x.GuildId == args.Guild.Id).ToListAsync();
            if (users.Count > 0)
            {
                ctx.GuildUsers.RemoveRange(users);
                changes = true;
            }

            var starboard = await ctx.Starboard.Where(x => x.GuildId == args.Guild.Id).ToListAsync();
            if (starboard.Count > 0)
            {
                ctx.Starboard.RemoveRange(starboard);
                changes = true;
            }

            var aliases = await ctx.CommandAliases.Where(x => x.GuildId == args.Guild.Id).ToListAsync();
            if (aliases.Count > 0)
            {
                ctx.CommandAliases.RemoveRange(aliases);
                changes = true;
            }

            var highlights = await ctx.Highlights.Where(x => x.GuildId == args.Guild.Id).ToListAsync();
            if (highlights.Count > 0)
            {
                ctx.Highlights.RemoveRange(highlights);
                changes = true;
            }

            var rewards = await ctx.LevelRewards.Where(x => x.GuildId == args.Guild.Id).ToListAsync();
            if (rewards.Count > 0)
            {
                ctx.LevelRewards.RemoveRange(rewards);
                changes = true;
            }

            var messages = await ctx.ModmailMessages.Include(x => x.Source).Where(x => x.Source.GuildId == args.Guild.Id).ToListAsync();
            if (messages.Count > 0)
            {
                ctx.ModmailMessages.RemoveRange(messages);
                changes = true;
            }

            var modmails = await ctx.Modmails.Where(x => x.GuildId == args.Guild.Id).ToListAsync();
            if (modmails.Count > 0)
            {
                ctx.Modmails.RemoveRange(modmails);
                changes = true;
            }

            var reminders = await ctx.Reminders.Where(x => x.GuildId == args.Guild.Id).ToListAsync();
            if (reminders.Count > 0)
            {
                ctx.Reminders.RemoveRange(reminders);
                changes = true;
            }

            var punishments = await ctx.Punishments.Where(x => x.GuildId == args.Guild.Id).ToListAsync();
            if (punishments.Count > 0)
            {
                ctx.Punishments.RemoveRange(punishments);
                changes = true;
            }

            var permissions = await ctx.Permissions.Where(x => x.GuildId == args.Guild.Id).ToListAsync();
            if (permissions.Count > 0)
            {
                ctx.Permissions.RemoveRange(permissions);
                changes = true;
            }

            var tags = await ctx.Tags.Where(x => x.GuildId == args.Guild.Id).ToListAsync();
            if (tags.Count > 0)
            {
                ctx.Tags.RemoveRange(tags);
                changes = true;
            }

            var warningPunishments = await ctx.WarningPunishments.Where(x => x.GuildId == args.Guild.Id).ToListAsync();
            if (warningPunishments.Count > 0)
            {
                ctx.WarningPunishments.RemoveRange(warningPunishments);
                changes = true;
            }

            var suggestions = await ctx.Suggestions.Where(x => x.GuildId == args.Guild.Id).ToListAsync();
            if (suggestions.Count > 0)
            {
                ctx.Suggestions.RemoveRange(suggestions);
                changes = true;
            }

            var emojis = await ctx.SpecialEmojis.Where(x => x.GuildId == args.Guild.Id).ToListAsync();
            if (emojis.Count > 0)
            {
                ctx.SpecialEmojis.RemoveRange(emojis);
                changes = true;
            }

            if (changes)
            {
                await ctx.SaveChangesAsync();
            }
        }
    }
}