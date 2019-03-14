using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Qmmands;

namespace Administrator.Commands
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class RequireHierarchyAttribute : ParameterCheckBaseAttribute
    {
        public override Task<CheckResult> CheckAsync(object argument, ICommandContext ctx, IServiceProvider provider)
        {
            var context = (AdminCommandContext) ctx;
            var target = (SocketGuildUser) argument;
            var user = (SocketGuildUser) context.User;

            if (context.IsPrivate)
                return Task.FromResult(
                    CheckResult.Unsuccessful(context.Localize("requirecontext", Format.Bold("guild"))));

            if (context.Guild.CurrentUser.Hierarchy <= target.Hierarchy)
                return Task.FromResult(CheckResult.Unsuccessful(context.Localize("requirehierarchy_self")));

            if (user.Hierarchy <= target.Hierarchy)
                return Task.FromResult(CheckResult.Unsuccessful(context.Localize("requirehierarchy_user")));

            return Task.FromResult(CheckResult.Successful);
        }
    }
}