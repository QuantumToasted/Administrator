using System.Threading.Tasks;
using Administrator.Common;
using Disqord;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class EmojiParser<TEmoji> : TypeParser<TEmoji>
        where TEmoji : IEmoji
    {
        public override ValueTask<TypeParserResult<TEmoji>> ParseAsync(Parameter parameter, string value, CommandContext ctx)
        {
            var context = (AdminCommandContext) ctx;
            return EmojiTools.TryParse(value, out var result)
                ? TypeParserResult<TEmoji>.Successful((TEmoji) result)
                : TypeParserResult<TEmoji>.Unsuccessful(context.Localize("emojiparser_notfound"));
        }
    }
}