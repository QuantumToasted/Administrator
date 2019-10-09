using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Administrator.Common;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class RequirePermissionsAttribute : CheckAttribute
    {
        public override async ValueTask<CheckResult> CheckAsync(CommandContext ctx)
        {
            var context = (AdminCommandContext) ctx;
            var permissions = context.IsPrivate
                ? await context.Database.Permissions.Where(x => !x.GuildId.HasValue)
                    .OrderByDescending(x => x.Id)
                    .ToListAsync()
                : await context.Database.Permissions.Where(x => !x.GuildId.HasValue || x.GuildId == context.Guild.Id)
                    .OrderByDescending(x => x.Id)
                    .ToListAsync();

            if (context.Guild?.OwnerId == context.User.Id)
                return CheckResult.Successful;

            var userId = context.User.Id;
            var roleIds = (context.User as SocketGuildUser)?.Roles.Select(x => x.Id).ToList()
                          ?? new List<ulong>();
            var channelId = context.Channel.Id;
            var guildId = context.Guild?.Id;
            var commandName = context.Command.FullAliases[0];

            foreach (var permission in permissions)
            {
                if (!string.IsNullOrWhiteSpace(permission.Name) &&
                    !context.Command.Module.Name.Equals(permission.Name) &&
                    !commandName.Equals(permission.Name))
                    continue;

                switch (permission.Filter)
                {
                    case PermissionFilter.Global:
                    case PermissionFilter.Guild when guildId == permission.TargetId:
                    case PermissionFilter.Role when roleIds.Contains(permission.TargetId.GetValueOrDefault()):
                    case PermissionFilter.Channel when channelId == permission.TargetId:
                    case PermissionFilter.User when userId == permission.TargetId:
                        return permission.IsEnabled
                            ? CheckResult.Successful
                            : CheckResult.Unsuccessful(context.Localize("permissions_denied", permission.Id));
                }
            }

            return CheckResult.Successful;
        }
    }
}