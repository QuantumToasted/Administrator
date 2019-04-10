using System;
using System.Threading.Tasks;
using Administrator.Common;
using Discord;
using Humanizer;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class RequireBotPermissionsAttribute : RequireContextAttribute
    {
        private readonly bool _isGuildPermissions;

        public RequireBotPermissionsAttribute(GuildPermission requiredGuildPermissions)
            : base(ContextType.Guild)
        {
            RequiredGuildPermissions = requiredGuildPermissions;
            _isGuildPermissions = true;
        }

        public RequireBotPermissionsAttribute(ChannelPermission requiredChannelPermissions)
            : base(ContextType.Guild)
        {
            RequiredChannelPermissions = requiredChannelPermissions;
        }

        public GuildPermission RequiredGuildPermissions { get; }

        public ChannelPermission RequiredChannelPermissions { get; }

        public override async ValueTask<CheckResult> CheckAsync(CommandContext ctx, IServiceProvider provider)
        {
            var baseCheck = await base.CheckAsync(ctx, provider);
            if (!baseCheck.IsSuccessful)
                return baseCheck;

            var context = (AdminCommandContext) ctx;
            if (_isGuildPermissions)
            {
                return !context.Guild.CurrentUser.GuildPermissions.Has(RequiredGuildPermissions)
                    ? CheckResult.Unsuccessful(context.Localize("requirebotpermissions_guild",
                        Format.Bold(RequiredGuildPermissions.Humanize(LetterCasing.Title))))
                    : CheckResult.Successful;
            }

            return !context.Guild.CurrentUser.GetPermissions(context.Channel as IGuildChannel)
                .Has(RequiredChannelPermissions)
                ? CheckResult.Unsuccessful(context.Localize("requirebotpermissions_channel",
                    Format.Bold(RequiredChannelPermissions.Humanize(LetterCasing.Title))))
                : CheckResult.Successful;
        }
    }
}