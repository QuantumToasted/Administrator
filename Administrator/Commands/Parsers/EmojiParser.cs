using System.Threading.Tasks;
using Administrator.Common;
using Disqord;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class EmojiParser : TypeParser<IEmoji>
    {
        public override ValueTask<TypeParserResult<IEmoji>> ParseAsync(Parameter parameter, string value, CommandContext ctx)
        {
            var context = (AdminCommandContext) ctx;
            return EmojiTools.TryParse(value, out var result)
                ? TypeParserResult<IEmoji>.Successful(result)
                : TypeParserResult<IEmoji>.Unsuccessful(context.Localize("emojiparser_notfound"));
        }
    }
}