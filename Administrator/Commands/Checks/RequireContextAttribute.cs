using System;
using System.Threading.Tasks;
using Administrator.Common;
using Qmmands;

namespace Administrator.Commands
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireContextAttribute : CheckAttribute
    {
        public RequireContextAttribute(ContextType requiredContext)
        {
            RequiredContext = requiredContext;
        }

        public ContextType RequiredContext { get; }

        public override ValueTask<CheckResult> CheckAsync(CommandContext ctx, IServiceProvider provider)
        {
            var context = (AdminCommandContext) ctx;
            var contextType = context.IsPrivate ? ContextType.DM : ContextType.Guild;

            if (RequiredContext == contextType)
                return CheckResult.Successful;

            return CheckResult.Unsuccessful(RequiredContext == ContextType.DM
                ? context.Localize("requirecontext_dm")
                : context.Localize("requirecontext_guild"));
        }
    }
}