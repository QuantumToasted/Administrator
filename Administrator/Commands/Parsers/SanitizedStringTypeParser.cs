using System.Threading.Tasks;
using Disqord.Bot;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class SanitizedStringTypeParser : DiscordTypeParser<string>
    {
        public override async ValueTask<TypeParserResult<string>> ParseAsync(Parameter parameter, string value, DiscordCommandContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}