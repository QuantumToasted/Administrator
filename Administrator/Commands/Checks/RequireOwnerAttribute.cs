using System;
using System.Threading.Tasks;
using Administrator.Services;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;

namespace Administrator.Commands
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class RequireOwnerAttribute : CheckBaseAttribute
    {
        public override Task<CheckResult> CheckAsync(ICommandContext ctx, IServiceProvider provider)
        {
            var context = (AdminCommandContext) ctx;
            var config = provider.GetRequiredService<ConfigurationService>();

            return Task.FromResult(config.OwnerIds.Contains(context.User.Id)
                ? CheckResult.Successful
                : CheckResult.Unsuccessful(context.Localize("requireowner")));
        }
    }
}