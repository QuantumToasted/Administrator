using System.Linq;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Common.LocalizedEmbed;
using Administrator.Database;
using Administrator.Extensions;
using Administrator.Services;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Qmmands;
using Permission = Disqord.Permission;

namespace Administrator.Commands.Modules.SelfRoles
{
    [Name("SelfRoles")]
    [Group("selfrole", "sr")]
    [RequireBotPermissions(Permission.ManageRoles)]
    public class SelfRoleCommands : AdminModuleBase
    {
        public PaginationService Pagination { get; set; }

        [Command("list")]
        public async ValueTask<AdminCommandResult> ListAsync()
        {
            var selfRoles = await Context.Database.SelfRoles.Where(x => x.GuildId == Context.Guild.Id)
                .ToListAsync();

            if (selfRoles.Count == 0)
                return CommandErrorLocalized("selfrole_list_none");

            var pages = DefaultPaginator.GeneratePages(selfRoles, 10, role => new LocalizedFieldBuilder(this)
                .WithName(Context.Guild.GetRole(role.RoleId).Format(false))
                .WithLocalizedValue("selfrole_list_groups",
                    role.Groups.Length > 0 ? string.Join(", ", role.Groups) : Localize("info_none")), builderFunc: () =>
                new LocalizedEmbedBuilder(this)
                    .WithSuccessColor()
                    .WithLocalizedTitle("selfrole_list_title", Context.Guild.Name.Sanitize()));

            if (pages.Count > 1)
            {
                await Pagination.SendPaginatorAsync(Context.Channel, new DefaultPaginator(pages, 0), pages[0]);
                return CommandSuccess();
            }

            return CommandSuccess(embed: pages[0].Embed);
        }

        [Command("add")]
        public async ValueTask<AdminCommandResult> AddAsync([Remainder] CachedRole role)
        {
            if (!(await Context.Database.SelfRoles.FindAsync(Context.Guild.Id.RawValue, role.Id.RawValue) is { }
                selfRole))
                return CommandErrorLocalized("selfrole_invalid");

            var member = (CachedMember) Context.User;
            if (member.Roles.ContainsKey(selfRole.RoleId))
                return CommandErrorLocalized("selfrole_add_exists");

            if (selfRole.Groups.Length > 0)
            {
                var groupRoles = await Context.Database.SelfRoles
                    .Where(x => x.GuildId == Context.Guild.Id && x.Groups.Length > 0)
                    .ToListAsync();

                foreach (var group in selfRole.Groups)
                {
                    if (member.Roles.Values.FirstOrDefault(x =>
                        groupRoles.Any(y => y.RoleId == x.Id && y.Groups.Contains(group))) is { } exclusiveRole)
                    {
                        return CommandErrorLocalized("selfrole_add_group_exists",
                            args: new object[]
                                {Markdown.Code(group.ToString()), Markdown.Bold(exclusiveRole.Name.Sanitize())});
                    }
                }

                // if (selfRole.Groups.FirstOrDefault(x => groupRoles.Any(y => y.Groups.Contains(x) && member.Roles.ContainsKey(y.RoleId))) is { } group)
                //     return CommandErrorLocalized("selfrole_add_group_exists", args: Markdown.Bold(group.ToString())); // Has a role in the group already
            }

            await member.GrantRoleAsync(selfRole.RoleId);
            return CommandSuccessLocalized("selfrole_add_success", args: Markdown.Bold(role.Name.Sanitize()));
        }

        [Command("remove")]
        public async ValueTask<AdminCommandResult> RemoveAsync([Remainder] CachedRole role)
        {
            if (!(await Context.Database.SelfRoles.FindAsync(Context.Guild.Id.RawValue, role.Id.RawValue) is { }
                selfRole))
                return CommandErrorLocalized("selfrole_invalid");

            var member = (CachedMember)Context.User;
            if (!member.Roles.ContainsKey(selfRole.RoleId))
                return CommandErrorLocalized("selfrole_remove_none");

            await member.RevokeRoleAsync(selfRole.RoleId);
            return CommandSuccessLocalized("selfrole_remove_success", args: Markdown.Bold(role.Name.Sanitize()));
        }

        [RequireUserPermissions(Permission.ManageRoles)]
        public class SelfRoleManagementCommands : SelfRoleCommands
        {
            [Command("create")]
            public async ValueTask<AdminCommandResult> CreateAsync([Remainder, RequireHierarchy] CachedRole role)
            {
                if (await Context.Database.SelfRoles.FindAsync(Context.Guild.Id.RawValue, role.Id.RawValue) is { })
                    return CommandErrorLocalized("selfrole_create_exists");

                Context.Database.SelfRoles.Add(new SelfAssignableRole(Context.Guild.Id, role.Id));
                await Context.Database.SaveChangesAsync();

                return CommandSuccessLocalized("selfrole_create_success", args: role.Format());
            }

            [Command("delete")]
            public async ValueTask<AdminCommandResult> DeleteAsync([Remainder] CachedRole role)
            {
                if (!(await Context.Database.SelfRoles.FindAsync(Context.Guild.Id.RawValue, role.Id.RawValue) is { }
                    selfRole))
                    return CommandErrorLocalized("selfrole_invalid");

                Context.Database.SelfRoles.Remove(selfRole);
                await Context.Database.SaveChangesAsync();

                return CommandSuccessLocalized("selfrole_delete_success", args: role.Format());
            }

            [Group("group")]
            public sealed class SelfRoleGroupCommands : SelfRoleManagementCommands
            {
                [Command("add")]
                public async ValueTask<AdminCommandResult> AddToGroupAsync([MustBe(Operator.GreaterThan, 0)] int group, 
                    [Remainder] CachedRole role)
                {
                    if (!(await Context.Database.SelfRoles.FindAsync(Context.Guild.Id.RawValue, role.Id.RawValue) is { }
                        selfRole))
                        return CommandErrorLocalized("selfrole_invalid");

                    if (selfRole.Groups.Contains(group))
                        return CommandErrorLocalized("selfrole_group_add_exists", args: Markdown.Code(group.ToString()));

                    selfRole.Groups = selfRole.Groups.Concat(new[] {group}).OrderBy(x => x).ToArray();
                    Context.Database.SelfRoles.Update(selfRole);
                    await Context.Database.SaveChangesAsync();

                    return CommandSuccessLocalized("selfrole_group_add_success",
                        args: new object[] {role.Format(), Markdown.Code(group.ToString())});
                }

                [Command("remove")]
                public async ValueTask<AdminCommandResult> RemoveFromGroupAsync([MustBe(Operator.GreaterThan, 0)] int group, 
                    [Remainder] CachedRole role)
                {
                    if (!(await Context.Database.SelfRoles.FindAsync(Context.Guild.Id.RawValue, role.Id.RawValue) is { }
                        selfRole))
                        return CommandErrorLocalized("selfrole_invalid");

                    if (!selfRole.Groups.Contains(group))
                        return CommandErrorLocalized("selfrole_group_remove_invalid", args: Markdown.Code(group.ToString()));

                    selfRole.Groups = selfRole.Groups.Except(new[] {group}).OrderBy(x => x).ToArray();
                    Context.Database.SelfRoles.Update(selfRole);
                    await Context.Database.SaveChangesAsync();

                    return CommandSuccessLocalized("selfrole_group_remove_success",
                        args: new object[] {role.Format(), Markdown.Code(group.ToString())});
                }
            }
        }
    }
}