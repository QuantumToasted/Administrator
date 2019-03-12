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
        private readonly bool isGuildPermissions;

        public RequireBotPermissionsAttribute(GuildPermission requiredGuildPermissions)
            : base(ContextType.Guild)
        {
            RequiredGuildPermissions = requiredGuildPermissions;
            isGuildPermissions = true;
        }

        public RequireBotPermissionsAttribute(ChannelPermission requiredChannelPermissions)
            : base(ContextType.Guild)
        {
            RequiredChannelPermissions = requiredChannelPermissions;
        }

        public GuildPermission RequiredGuildPermissions { get; }

        public ChannelPermission RequiredChannelPermissions { get; }

        public override async Task<CheckResult> CheckAsync(ICommandContext ctx, IServiceProvider provider)
        {
            var baseCheck = await base.CheckAsync(ctx, provider);
            if (!baseCheck.IsSuccessful)
                return baseCheck;

            var context = (AdminCommandContext) ctx;
            if (isGuildPermissions)
            {
                return context.Guild.CurrentUser.GuildPermissions.Has(RequiredGuildPermissions)
                    ? CheckResult.Successful
                    : CheckResult.Unsuccessful(context.Language.Localize("requirebotpermissions_guild",
                        Format.Bold(RequiredGuildPermissions.Humanize(LetterCasing.Title))));
            }

            return context.Guild.CurrentUser.GetPermissions(context.Channel as IGuildChannel)
                .Has(RequiredChannelPermissions)
                ? CheckResult.Successful
                : CheckResult.Unsuccessful(context.Language.Localize("requirebotpermissions_channel",
                    Format.Bold(RequiredChannelPermissions.Humanize(LetterCasing.Title))));
        }
    }
}