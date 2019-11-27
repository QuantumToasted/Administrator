using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Disqord;
using Disqord.Rest;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class JumpLinkParser : TypeParser<RestUserMessage>
    {
        public static readonly Regex JumpLinkRegex = new Regex(@"https:\/\/discordapp\.com\/channels\/(@me|\d{16,18})\/(\d{16,18})\/(\d{16,18})", RegexOptions.Compiled);

        public override async ValueTask<TypeParserResult<RestUserMessage>> ParseAsync(Parameter parameter, string value, CommandContext ctx)
        {
            var context = (AdminCommandContext) ctx;

            var match = JumpLinkRegex.Match(value);
            if (match.Success && Snowflake.TryParse(match.Groups[1].Value, out var channelId) && 
                Snowflake.TryParse(match.Groups[2].Value, out var messageId) &&
                context.Client.GetChannel(channelId) is IMessageChannel channel)
            {
                return !(await channel.GetMessageAsync(messageId) is RestUserMessage message)
                    ? TypeParserResult<RestUserMessage>.Unsuccessful(context.Localize(""))
                    : TypeParserResult<RestUserMessage>.Successful(message);
            }

            return TypeParserResult<RestUserMessage>.Unsuccessful(context.Localize(""));
        }
    }
}