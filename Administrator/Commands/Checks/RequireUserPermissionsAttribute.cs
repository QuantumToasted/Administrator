using System;
using System.Threading.Tasks;
using Administrator.Common;
using Discord;
using Discord.WebSocket;
using Humanizer;
using Qmmands;

namespace Administrator.Commands
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public sealed class RequireUserPermissionsAttribute : RequireContextAttribute
    {
        private readonly bool _isGuildPermissions;

        public RequireUserPermissionsAttribute(GuildPermission requiredGuildPermissions)
            : base(ContextType.Guild)
        {
            RequiredGuildPermissions = requiredGuildPermissions;
            _isGuildPermissions = true;
        }

        public RequireUserPermissionsAttribute(ChannelPermission requiredChannelPermissions)
            : base(ContextType.Guild)
        {
            RequiredChannelPermissions = requiredChannelPermissions;
        }

        public GuildPermission RequiredGuildPermissions { get; }

        public ChannelPermission RequiredChannelPermissions { get; }

        public override async ValueTask<CheckResult> CheckAsync(CommandContext ctx)
        {
            var baseCheck = await base.CheckAsync(ctx);
            if (!baseCheck.IsSuccessful)
                return baseCheck;

            var context = (AdminCommandContext) ctx;
            var user = (SocketGuildUser) context.User;

            if (_isGuildPermissions)
            {
                return !user.GuildPermissions.Has(RequiredGuildPermissions)
                    ? CheckResult.Unsuccessful(context.Localize("requireuserpermissions_guild",
                        Format.Bold(RequiredGuildPermissions.Humanize(LetterCasing.Title))))
                    : CheckResult.Successful;
            }

            return !user.GuildPermissions.Has(RequiredGuildPermissions)
                ? CheckResult.Unsuccessful(context.Localize("requireuserpermissions_guild",
                    Format.Bold(RequiredChannelPermissions.Humanize(LetterCasing.Title))))
                : CheckResult.Successful;
        }
    }
}