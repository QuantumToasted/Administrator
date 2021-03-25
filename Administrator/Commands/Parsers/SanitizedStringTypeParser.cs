using System.Linq;
using System.Threading.Tasks;
using Disqord.Bot;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class SanitizedStringTypeParser : DiscordTypeParser<string>
    {
        public override ValueTask<TypeParserResult<string>> ParseAsync(Parameter parameter, string value, DiscordCommandContext context)
        {
            value = parameter.Attributes.OfType<SanitaryTextAttribute>().Aggregate(value,
                (val, attribute) => attribute.Modification.Invoke(val));

            return Success(value);
        }
    }
}