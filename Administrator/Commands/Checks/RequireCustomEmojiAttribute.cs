using System;
using Qmmands;
using System.Threading.Tasks;
using Disqord;

namespace Administrator.Commands
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class RequireCustomEmojiAttribute : ParameterCheckAttribute
    {
        public override ValueTask<CheckResult> CheckAsync(object argument, CommandContext ctx)
        {
            var context = (AdminCommandContext) ctx;
            return !LocalCustomEmoji.TryParse(((IEmoji) argument).MessageFormat, out _)
                ? CheckResult.Unsuccessful(context.Localize("require_custom_emoji"))
                : CheckResult.Successful;
        }
    }
}