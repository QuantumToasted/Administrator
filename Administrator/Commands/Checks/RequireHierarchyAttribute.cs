using System;
using System.Threading.Tasks;
using Administrator.Extensions;
using Disqord;
using Qmmands;

namespace Administrator.Commands
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class RequireHierarchyAttribute : ParameterCheckAttribute
    {
        public override ValueTask<CheckResult> CheckAsync(object argument, CommandContext ctx)
        {
            var context = (AdminCommandContext) ctx;
            var user = (CachedMember) context.User;

            if (context.IsPrivate)
                return CheckResult.Unsuccessful(context.Localize("requirecontext_guild"));

            var result = string.Empty;
            switch (argument)
            {
                case CachedRole targetRole:
                    result = GetRoleResult(targetRole);
                    break;
                case CachedRole[] targetRoles:
                    foreach (var targetRole in targetRoles)
                    {
                        result = GetRoleResult(targetRole);
                        if (!string.IsNullOrWhiteSpace(result))
                        {
                            break;
                        }
                    }

                    break;
                case CachedMember[] targetUsers:
                    foreach (var targetUser in targetUsers)
                    {
                        result = GetUserResult(targetUser);
                        if (!string.IsNullOrWhiteSpace(result))
                            break;
                    }

                    break;
                case CachedMember targetUser:
                    result = GetUserResult(targetUser);
                    break;
            }

            return string.IsNullOrWhiteSpace(result)
                ? CheckResult.Successful
                : CheckResult.Unsuccessful(result);
            
            string GetRoleResult(CachedRole target)
            {
                if (context.Guild.CurrentMember.GetHighestRole().Position <= target.Position)
                    return context.Localize("role_unassignable_self", Markdown.Bold(target.Name));
                if (user.GetHighestRole().Position <= target.Position)
                    return context.Localize("role_unassignable_user", Markdown.Bold(target.Name));

                return string.Empty;
            }

            string GetUserResult(CachedMember target)
            {
                if (context.Guild.CurrentMember.Hierarchy <= target.Hierarchy)
                    return context.Localize("requirehierarchy_self");
                if (user.Hierarchy <= target.Hierarchy)
                    return context.Localize("requirehierarchy_user");

                return string.Empty;
            }
        }
    }
}