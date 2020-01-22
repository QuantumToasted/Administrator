using System;
using System.Threading.Tasks;
using Humanizer;
using Administrator.Common;
using Disqord;
using Qmmands;

namespace Administrator.Commands
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public sealed class RequireBotPermissionsAttribute : RequireContextAttribute
    {
        private readonly bool _isGuildPermissions;

        public RequireBotPermissionsAttribute(Permission requiredPermissions, bool isGuildPermissions = true)
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

            var context = (AdminCommandContext) ctx;
            if (_isGuildPermissions)
            {
                return !context.Guild.CurrentMember.Permissions.Has(RequiredPermissions)
                    ? CheckResult.Unsuccessful(context.Localize("requirebotpermissions_guild",
                        Markdown.Bold(RequiredPermissions.Humanize(LetterCasing.Title))))
                    : CheckResult.Successful;
            }

            return !context.Guild.CurrentMember.GetPermissionsFor(context.Channel as IGuildChannel)
                .Has(RequiredPermissions)
                ? CheckResult.Unsuccessful(context.Localize("requirebotpermissions_channel",
                    Markdown.Bold(RequiredPermissions.Humanize(LetterCasing.Title))))
                : CheckResult.Successful;
        }
    }
}