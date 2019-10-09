using System;
using System.Threading.Tasks;
using Qmmands;

namespace Administrator.Commands
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class RequireOwnerAttribute : CheckAttribute
    {
        public override async ValueTask<CheckResult> CheckAsync(CommandContext ctx)
        {
            var context = (AdminCommandContext) ctx;
            var app = await context.Client.GetApplicationInfoAsync();

            return context.User.Id != app.Owner.Id
                ? CheckResult.Unsuccessful(context.Localize("requireowner"))
                : CheckResult.Successful;
        }
    }
}