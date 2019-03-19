using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Qmmands;

namespace Administrator.Commands
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class RequireHierarchyAttribute : ParameterCheckAttribute
    {
        public override ValueTask<CheckResult> CheckAsync(object argument, CommandContext ctx, IServiceProvider provider)
        {
            var context = (AdminCommandContext) ctx;
            var target = (SocketGuildUser) argument;
            var user = (SocketGuildUser) context.User;

            if (context.IsPrivate)
                return CheckResult.Unsuccessful(context.Localize("requirecontext", Format.Bold("guild")));

            if (context.Guild.CurrentUser.Hierarchy <= target.Hierarchy)
                return CheckResult.Unsuccessful(context.Localize("requirehierarchy_self"));

            if (user.Hierarchy <= target.Hierarchy)
                return CheckResult.Unsuccessful(context.Localize("requirehierarchy_user"));

            return CheckResult.Successful;
        }
    }
}