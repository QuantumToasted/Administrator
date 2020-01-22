using System;
using System.Linq;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Extensions;
using Disqord;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class EmojiParser : TypeParser<IEmoji>
    {
        public override ValueTask<TypeParserResult<IEmoji>> ParseAsync(Parameter parameter, string value, CommandContext ctx)
        {
            var context = (AdminCommandContext) ctx;
            var random = context.ServiceProvider.GetRequiredService<Random>();
            var validSearchableEmojis = context.Client.Guilds.Values.SelectMany(x => x.Emojis.Values).ToList();

            if (!EmojiTools.TryParse(value, out var emoji))
            {
                if (Snowflake.TryParse(value, out var id))
                {
                    emoji = validSearchableEmojis.FirstOrDefault(x => x.Id == id);
                }
                else
                {
                    var matchingEmojis = validSearchableEmojis
                        .Where(x => x.Name.Equals(value, StringComparison.OrdinalIgnoreCase)).ToList();

                    if (matchingEmojis.Count > 0 && !parameter.Checks.OfType<RequireGuildEmojiAttribute>().Any())
                    {
                        emoji = matchingEmojis.GetRandomElement(random);
                    }
                }
            }

            return !(emoji is null)
                ? TypeParserResult<IEmoji>.Successful(emoji)
                : TypeParserResult<IEmoji>.Unsuccessful(context.Localize("emojiparser_notfound"));
        }
    }
}