using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Extensions;
using Administrator.Services;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Qmmands;
using Permission = Administrator.Database.Permission;

namespace Administrator.Commands
{
    [Name("Permissions")]
    [Group("permissions", "perms")]
    public class PermissionCommands : AdminModuleBase
    {
        public CommandService Commands { get; set; }

        public PaginationService Pagination { get; set; }

        [Group("enable", "disable")]
        public class PermissionCreationCommands : PermissionCommands
        {
            [Group("command", "module")]
            public class SpecificPermissionCommands : PermissionCreationCommands
            {
                public ConfigurationService Config { get; set; }

                [Command]
                public ValueTask<AdminCommandResult> AddPermission(string name)
                    => AddPermissionAsync(name, Context.Path[1].Equals("enable"),
                        Context.IsPrivate && Config.OwnerIds.Contains(Context.User.Id)
                            ? PermissionFilter.Global
                            : PermissionFilter.Guild, null);

                [Command]
                public ValueTask<AdminCommandResult> AddPermission(string name,
                    [Remainder, RequireHierarchy] CachedMember user)
                    => AddPermissionAsync(name, Context.Path[1].Equals("enable"), PermissionFilter.User, user.Id);

                [Command]
                public ValueTask<AdminCommandResult> AddPermission(string name,
                    [Remainder] CachedRole role)
                    => AddPermissionAsync(name, Context.Path[1].Equals("enable"), PermissionFilter.Role, role.Id);

                [Command]
                public ValueTask<AdminCommandResult> AddPermission(string name,
                    [Remainder] CachedTextChannel channel)
                    => AddPermissionAsync(name, Context.Path[1].Equals("enable"), PermissionFilter.Channel, channel.Id);
            }

            [Group("all")]
            public class AllPermissionCommands : PermissionCreationCommands
            {
                public ConfigurationService Config { get; set; }

                [Command]
                public ValueTask<AdminCommandResult> AddPermission()
                    => AddPermissionAsync(null, Context.Path[1].Equals("enable"),
                        Context.IsPrivate && Config.OwnerIds.Contains(Context.User.Id)
                            ? PermissionFilter.Global
                            : PermissionFilter.Guild, null);

                [Command]
                public ValueTask<AdminCommandResult> AddPermission([Remainder, RequireHierarchy] CachedMember user)
                    => AddPermissionAsync(null, Context.Path[1].Equals("enable"), PermissionFilter.User, user.Id);

                [Command]
                public ValueTask<AdminCommandResult> AddPermission([Remainder] CachedRole role)
                    => AddPermissionAsync(null, Context.Path[1].Equals("enable"), PermissionFilter.Role, role.Id);

                [Command]
                public ValueTask<AdminCommandResult> AddPermission([Remainder] CachedTextChannel channel)
                    => AddPermissionAsync(null, Context.Path[1].Equals("enable"), PermissionFilter.Channel, channel.Id);
            }
        }

        private async ValueTask<AdminCommandResult> AddPermissionAsync(string name, bool isEnabled,
            PermissionFilter filter, ulong? targetId)
        {
            Permission permission;
            var type = Enum.Parse<PermissionType>(Context.Alias, true);
            if (type == PermissionType.Command)
            {
                var commandMatches = Commands.FindCommands(name);
                if (commandMatches.Count == 0)
                    return CommandErrorLocalized("permissions_command_notfound");

                permission = new Permission(Context.Guild?.Id, type, 
                    isEnabled, string.Join(' ', commandMatches[0].Command.FullAliases[0]).ToLower(), filter, targetId);
            }
            else
            {
                if (!(Commands.GetAllModules()
                    .FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) is { } module))
                    return CommandErrorLocalized("permissions_module_notfound"); 

                permission = new Permission(Context.Guild?.Id, type, 
                    isEnabled, module.Name.ToLower(), filter, targetId);
            }

            Context.Database.Permissions.Add(permission);
            await Context.Database.SaveChangesAsync();

            var filterText = filter switch
            {
                PermissionFilter.Global => Context.Localize("permissions_global"),
                PermissionFilter.Guild => Context.Guild.Name.Sanitize(),
                PermissionFilter.Role => Context.Guild.GetRole(targetId.Value).Name.Sanitize(),
                PermissionFilter.Channel => Context.Guild.GetTextChannel(targetId.Value).Mention,
                PermissionFilter.User => Context.Guild.GetMember(targetId.Value)?.Tag.Sanitize() ?? "???",
                _ => throw new ArgumentOutOfRangeException()
            };

            if (string.IsNullOrWhiteSpace(name))
            {
                return CommandSuccessLocalized(permission.IsEnabled
                    ? "permissions_all_enabled"
                    : "permissions_all_disabled", args: filterText);
            }

            switch (permission.Type)
            {
                case PermissionType.Command:
                    return CommandSuccessLocalized(permission.IsEnabled
                        ? "permissions_command_enabled"
                        : "permissions_command_disabled", args: new object[]
                    {
                        Markdown.Code(permission.Name),
                        Markdown.Bold(filterText)
                    });
                case PermissionType.Module:
                    return CommandSuccessLocalized(permission.IsEnabled
                        ? "permissions_module_enabled"
                        : "permissions_module_disabled", args: new object[]
                    {
                        Markdown.Code(permission.Name),
                        Markdown.Bold(filterText)
                    });
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        [Command("", "list")]
        public async ValueTask<AdminCommandResult> ListPermissionAsync([MustBe(Operator.GreaterThan, 0)] int page = 1)
        {
            var permissions = await Context.Database.Permissions.Where(x => x.GuildId == Context.Guild.Id)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            if (permissions.Count == 0)
                return CommandErrorLocalized("permissions_none");

            var pages = DefaultPaginator.GeneratePages(permissions, lineFunc: Format,
                builderFunc: () => new LocalEmbedBuilder()
                .WithSuccessColor()
                .WithTitle(Context.Localize("permissions_list", Context.Guild?.Name)));

            page = Math.Min(page, pages.Count) - 1;

            if (pages.Count > 1)
            {
                await Pagination.SendPaginatorAsync(Context.Channel, new DefaultPaginator(pages, page), pages[page]);
                return CommandSuccess();
            }

            return CommandSuccess(embed: pages[0].Embed);

            string Format(Permission permission)
            {
                var builder = new StringBuilder($"{permission.Id}. ")
                    .Append('`')
                    .Append(Context.Prefix)
                    .Append($"{Context.Path[0]} ")
                    .Append(permission.IsEnabled ? "enable " : "disable ")
                    .Append(permission.Type == PermissionType.Command ? "command " : "module ")
                    .Append($"{permission.Name ?? "all"} ")
                    .Append(permission.Filter switch
                    {
                        PermissionFilter.Role => Context.Guild.GetRole(permission.TargetId.Value)?.Name.Sanitize() ?? "???",
                        PermissionFilter.Channel => Context.Guild.GetTextChannel(permission.TargetId.Value)?.Mention ?? "???",
                        PermissionFilter.User => Context.Guild.GetMember(permission.TargetId.Value)?.Tag.Sanitize() ?? "???",
                        _ => string.Empty
                    })
                    .Append('`');
                return builder.ToString();
            }
        }

        [Command("remove", "delete")]
        public async ValueTask<AdminCommandResult> RemovePermissionAsync([MustBe(Operator.GreaterThan, 0)] int id)
        {
            if (!(await Context.Database.Permissions.FirstOrDefaultAsync(x =>
                x.GuildId == Context.Guild.Id && x.Id == id) is { } permission))
                return CommandErrorLocalized("permissions_notfound");

            Context.Database.Permissions.Remove(permission);
            await Context.Database.SaveChangesAsync();

            return CommandSuccessLocalized("permissions_removed");
        }
    }
}