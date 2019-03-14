using System;
using System.Threading.Tasks;
using Administrator.Common;
using Discord;
using Discord.WebSocket;
using Humanizer;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class RequireUserPermissionsAttribute : RequireContextAttribute
    {
        private readonly bool isGuildPermissions;

        public RequireUserPermissionsAttribute(GuildPermission requiredGuildPermissions)
            : base(ContextType.Guild)
        {
            RequiredGuildPermissions = requiredGuildPermissions;
            isGuildPermissions = true;
        }

        public RequireUserPermissionsAttribute(ChannelPermission requiredChannelPermissions)
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
            var user = (SocketGuildUser) context.User;

            if (isGuildPermissions)
            {
                return user.GuildPermissions.Has(RequiredGuildPermissions)
                    ? CheckResult.Successful
                    : CheckResult.Unsuccessful(context.Localize("requireuserpermissions_guild",
                        Format.Bold(RequiredGuildPermissions.Humanize(LetterCasing.Title))));
            }

            return user.GetPermissions(context.Channel as IGuildChannel)
                .Has(RequiredChannelPermissions)
                ? CheckResult.Successful
                : CheckResult.Unsuccessful(context.Localize("requireuserpermissions_channel",
                    Format.Bold(RequiredChannelPermissions.Humanize(LetterCasing.Title))));
        }
    }
}