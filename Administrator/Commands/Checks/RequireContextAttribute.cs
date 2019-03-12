using System;
using System.Threading.Tasks;
using Administrator.Common;
using Qmmands;

namespace Administrator.Commands
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireContextAttribute : CheckBaseAttribute
    {
        public RequireContextAttribute(ContextType requiredContext)
        {
            RequiredContext = requiredContext;
        }

        public ContextType RequiredContext { get; }

        public override Task<CheckResult> CheckAsync(ICommandContext ctx, IServiceProvider provider)
        {
            var context = (AdminCommandContext) ctx;
            var contextType = context.IsPrivate ? ContextType.DM : ContextType.Guild;

            if (RequiredContext == contextType)
                return Task.FromResult(CheckResult.Successful);

            return Task.FromResult(CheckResult.Unsuccessful(RequiredContext == ContextType.DM
                ? "requirecontext_dm"
                : "requirecontext_guild"));
        }
    }
}