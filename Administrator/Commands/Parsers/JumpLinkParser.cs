using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Disqord;
using Disqord.Rest;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class JumpLinkParser : TypeParser<RestUserMessage>
    {
        public static readonly Regex JumpLinkRegex = new Regex(@"^https?://(?:(ptb|canary)\.)?discordapp\.com/channels/(?<guild_id>([0-9]{15,21})|(@me))/(?<channel_id>[0-9]{15,21})/(?<message_id>[0-9]{15,21})/?$", 
            RegexOptions.Compiled);

        public override async ValueTask<TypeParserResult<RestUserMessage>> ParseAsync(Parameter parameter, string value, CommandContext ctx)
        {
            var context = (AdminCommandContext) ctx;

            var match = JumpLinkRegex.Match(value);
            if (match.Success && Snowflake.TryParse(match.Groups[2].Value, out var channelId) && 
                Snowflake.TryParse(match.Groups[3].Value, out var messageId) &&
                context.Guild.GetTextChannel(channelId) is ITextChannel channel)
            {
                return !(await channel.GetMessageAsync(messageId) is RestUserMessage message)
                    ? TypeParserResult<RestUserMessage>.Unsuccessful(context.Localize("jumplinkparser_nomessage"))
                    : TypeParserResult<RestUserMessage>.Successful(message);
            }

            return TypeParserResult<RestUserMessage>.Unsuccessful(context.Localize("jumplinkparser_format"));
        }
    }
}