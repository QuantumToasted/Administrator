using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Administrator.Extensions;
using Discord;
using Qmmands;

namespace Administrator.Commands
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class RequireHierarchyAttribute : ParameterCheckAttribute
    {
        public override ValueTask<CheckResult> CheckAsync(object argument, CommandContext ctx, IServiceProvider provider)
        {
            var context = (AdminCommandContext) ctx;
            var user = (SocketGuildUser) context.User;

            if (context.IsPrivate)
                return CheckResult.Unsuccessful(context.Localize("requirecontext_guild"));

            var result = string.Empty;
            switch (argument)
            {
                case SocketRole targetRole:
                    result = GetRoleResult(targetRole);
                    break;
                case SocketRole[] targetRoles:
                    foreach (var targetRole in targetRoles)
                    {
                        result = GetRoleResult(targetRole);
                        if (!string.IsNullOrWhiteSpace(result))
                        {
                            break;
                        }
                    }

                    break;
                case SocketGuildUser[] targetUsers:
                    foreach (var targetUser in targetUsers)
                    {
                        result = GetUserResult(targetUser);
                        if (!string.IsNullOrWhiteSpace(result))
                            break;
                    }

                    break;
                case SocketGuildUser targetUser:
                    result = GetUserResult(targetUser);
                    break;
            }

            return string.IsNullOrWhiteSpace(result)
                ? CheckResult.Successful
                : CheckResult.Unsuccessful(result);
            
            string GetRoleResult(SocketRole target)
            {
                if (context.Guild.CurrentUser.GetHighestRole().Position <= target.Position)
                    return context.Localize("role_unassignable_self", Format.Bold(target.Name));
                if (user.GetHighestRole().Position <= target.Position)
                    return context.Localize("role_unassignable_user", Format.Bold(target.Name));

                return string.Empty;
            }

            string GetUserResult(SocketGuildUser target)
            {
                if (context.Guild.CurrentUser.Hierarchy <= target.Hierarchy)
                    return context.Localize("requirehierarchy_self");
                if (user.Hierarchy <= target.Hierarchy)
                    return context.Localize("requirehierarchy_user");

                return string.Empty;
            }
        }
    }
}