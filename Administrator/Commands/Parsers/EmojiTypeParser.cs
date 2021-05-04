using System;
using System.Linq;
using System.Threading.Tasks;
using Administrator.Database;
using Administrator.Extensions;
using Administrator.Services;
using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class EmojiTypeParser<TEmoji> : DiscordTypeParser<TEmoji>
        where TEmoji : IEmoji
    {
        private Random _random;
        private EmojiService _emojiService;
        
        public override async ValueTask<TypeParserResult<TEmoji>> ParseAsync(Parameter parameter, string value, DiscordCommandContext context)
        {
            _random ??= context.Bot.Services.GetRequiredService<Random>();
            _emojiService ??= context.Bot.Services.GetRequiredService<EmojiService>();

            if (typeof(TEmoji) == typeof(IGuildEmoji) && context.GuildId.HasValue &&
                context.Bot.GetGuild(context.GuildId.Value) is { } guild)
            {
                IGuildEmoji guildEmoji;
                if (LocalCustomEmoji.TryParse(value, out var customEmoji))
                {
                    guildEmoji = guild.Emojis.Values.FirstOrDefault(x => x.Id == customEmoji.Id);
                }
                else if (!Snowflake.TryParse(value, out var id) ||
                    !guild.Emojis.TryGetValue(id, out guildEmoji))
                {
                    var matchingGuildEmojis = guild.Emojis.Values.Where(x =>
                        x.Name.Equals(value, StringComparison.InvariantCultureIgnoreCase)).ToList();

                    guildEmoji = matchingGuildEmojis.Count > 0 ? matchingGuildEmojis.GetRandomElement(_random) : null;
                }

                if (guildEmoji is not null)
                    return Success((TEmoji) guildEmoji);
            }
            else
            {
                guild = null;
            }
            
            if (_emojiService.TryParseEmoji(value, out var parsedEmoji) && parsedEmoji is TEmoji emoji)
            {
                return Success(emoji);
            }

            using var scope = context.Bot.Services.CreateScope();
            await using var ctx = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
            
            var allEmojis = await ctx.GetAllBigEmojisAsync();
            var approvedEmojis = allEmojis.OfType<ApprovedBigEmoji>().ToList();
            var approvedIds = approvedEmojis.Select(x => x.Id).ToList();

            var guildEmojis = (context as DiscordGuildCommandContext)?.Guild?.Emojis.Values.Where(x =>
                x.Name.Equals(value, StringComparison.InvariantCultureIgnoreCase) ||
                Snowflake.TryParse(value, out var i) && x.Id == i).OfType<TEmoji>().ToList();

            if (guildEmojis?.Count > 0)
            {
                return Success(guildEmojis.GetRandomElement(_random));
            }

            if (!context.Bot.CacheProvider.TryGetGuilds(out var guilds))
                return Failure("The guild cache is not currently available to fetch available emojis.");

            if (guild is not null)
            {
                var dbGuild = await ctx.GetOrCreateGuildAsync(guild);
                if (dbGuild.BlacklistedEmojiNames.Contains(value, StringComparer.InvariantCultureIgnoreCase))
                {
                    return Failure("The provided emoji name has been blacklisted by this server.");
                }
            }

            var matchingEmojis = guilds.Values.SelectMany(x => x.Emojis.Values)
                .Where(x => approvedIds.Contains(x.Id) &&
                            x.Name.Equals(value, StringComparison.InvariantCultureIgnoreCase)).Cast<TEmoji>().ToList();

            if (matchingEmojis.Count > 0)
            {
                return Success(matchingEmojis.GetRandomElement(_random));
            }

            return Failure("An emoji could not be found with that name or formatting.\n" +
                           "(You may need to request the emoji be added to the whitelist if it hasn't been already.)");
        }
    }
}