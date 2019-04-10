using System;
using System.Threading.Tasks;
using Administrator.Services;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;

namespace Administrator.Commands
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class RequireOwnerAttribute : CheckAttribute
    {
        public override ValueTask<CheckResult> CheckAsync(CommandContext ctx, IServiceProvider provider)
        {
            var context = (AdminCommandContext) ctx;
            var config = provider.GetRequiredService<ConfigurationService>();

            return !config.OwnerIds.Contains(context.User.Id)
                ? CheckResult.Unsuccessful(context.Localize("requireowner"))
                : CheckResult.Successful;
        }
    }
}