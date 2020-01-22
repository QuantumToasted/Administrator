using System.Threading.Tasks;
using Administrator.Extensions;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class NotExecutingAttribute : CheckAttribute
    {
        public override ValueTask<CheckResult> CheckAsync(CommandContext context)
            => context.Command.IsExecuting((AdminCommandContext) context)
                ? CheckResult.Unsuccessful(string.Empty)
                : CheckResult.Successful;
    }
}