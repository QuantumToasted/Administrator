using System;
using System.Threading.Tasks;
using Administrator.Common;
using Disqord;
using Humanizer;
using Qmmands;

namespace Administrator.Commands
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public sealed class RequireUserPermissionsAttribute : RequireContextAttribute
    {
        private readonly bool _isGuildPermissions;

        public RequireUserPermissionsAttribute(Permission requiredPermissions, bool isGuildPermissions = true)
            : base(ContextType.Guild)
        {
            RequiredPermissions = requiredPermissions;
            _isGuildPermissions = isGuildPermissions;
        }

        public Permission RequiredPermissions { get; }

        public override async ValueTask<CheckResult> CheckAsync(CommandContext ctx)
        {
            var baseCheck = await base.CheckAsync(ctx);
            if (!baseCheck.IsSuccessful)
                return baseCheck;

            var context = (AdminCommandContext)ctx;
            var user = (CachedMember)context.User;

            if (_isGuildPermissions)
            {
                return !user.Permissions.Has(RequiredPermissions)
                    ? CheckResult.Unsuccessful(context.Localize("requireuserpermissions_guild",
                        Markdown.Bold(RequiredPermissions.Humanize(LetterCasing.Title))))
                    : CheckResult.Successful;
            }

            return !user.GetPermissionsFor(context.Channel as IGuildChannel)
                .Has(RequiredPermissions)
                ? CheckResult.Unsuccessful(context.Localize("requireuserpermissions_channel",
                    Markdown.Bold(RequiredPermissions.Humanize(LetterCasing.Title))))
                : CheckResult.Successful;
        }
    }
}