using System;
using System.Threading.Tasks;
using Administrator.Common;
using Qmmands;

namespace Administrator.Commands
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireContextAttribute : CheckBaseAttribute
    {
        public RequireContextAttribute(ContextType expectedContext)
        {
            ExpectedContext = expectedContext;
        }

        public ContextType ExpectedContext { get; }

        public override Task<CheckResult> CheckAsync(ICommandContext ctx, IServiceProvider provider)
        {
            var context = (AdminCommandContext) ctx;
            var contextType = context.IsPrivate ? ContextType.DM : ContextType.Guild;

            return Task.FromResult(ExpectedContext.HasFlag(contextType)
                ? CheckResult.Successful
                : CheckResult.Unsuccessful(
                    context.Language.Localize("requirecontext",
                        Format.Bold(context.Language.Localize(contextType.ToString().ToLower())))));
        }
    }
}