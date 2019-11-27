using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class RequireUsableEmojiAttribute : ParameterCheckAttribute
    {
        public override ValueTask<CheckResult> CheckAsync(object argument, CommandContext ctx)
        {
            var context = (AdminCommandContext) ctx;

            if (argument is CustomEmoji emoji)
            {
                return !context.Client.Guilds.Values.SelectMany(x => x.Emojis.Keys).Contains(emoji.Id)
                    ? CheckResult.Unsuccessful(context.Localize("requireusableemoji"))
                    : CheckResult.Successful;
            }

            return CheckResult.Successful;
        }
    }
}