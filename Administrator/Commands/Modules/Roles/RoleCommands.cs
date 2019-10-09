using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Administrator.Extensions;
using Administrator.Services;
using Discord;
using Discord.WebSocket;
using Humanizer;
using Qmmands;
using Direction = Administrator.Common.Direction;

namespace Administrator.Commands
{
    [Name("Roles")]
    [Group("role")]
    public class RoleCommands : AdminModuleBase
    {
        public ConfigurationService Config { get; set; }

        [RequireUserPermissions(GuildPermission.ManageRoles)]
        [RequireBotPermissions(GuildPermission.ManageRoles)]
        public sealed class ManageRoleCommands : RoleCommands
        {
            [Command("create")]
            public ValueTask<AdminCommandResult> CreateRole([Remainder] string name)
                => CreateRoleAsync(name, null);

            [Command("create")]
            public ValueTask<AdminCommandResult> CreateRole(string name, [Remainder] Color color)
                => CreateRoleAsync(name, color);

            private async ValueTask<AdminCommandResult> CreateRoleAsync(string name, Color? color)
            {
                var role = await Context.Guild.CreateRoleAsync(name, color: color);
                return color.HasValue
                    ? CommandSuccessLocalized("role_create_success_color",
                        args: new object[] { role.Format(), $"#{role.Color.RawValue:X}" })
                    : CommandSuccessLocalized("role_create_success", args: role.Format());
            }

            [Command("delete")]
            public async ValueTask<AdminCommandResult> DeleteRoleAsync([Remainder, RequireHierarchy] SocketRole role)
            {
                await role.DeleteAsync();
                return CommandSuccessLocalized("role_delete_success", args: role.Format());
            }

            [Command("color")]
            public async ValueTask<AdminCommandResult> ModifyRoleColorAsync([RequireHierarchy] SocketRole role,
            [Remainder] Color color)
            {
                await role.ModifyAsync(x => x.Color = color);
                return color == Color.Default
                    ? CommandSuccessLocalized("role_color_modified_default", args: role.Format())
                    : CommandSuccessLocalized("role_color_modified", args: new object[] { role.Format(), $"#{color.RawValue:X}" });
            }

            [Command("give", "grant")]
            public async ValueTask<AdminCommandResult> GiveRoleToUserAsync(SocketGuildUser target,
                [RequireHierarchy] params SocketRole[] roles)
            {
                if (roles.Length == 0)
                    throw new ArgumentOutOfRangeException(); // TODO: ask quahu how params T[] works

                if (roles.Length == 1 && target.Roles.Any(x => x.Id == roles[0].Id))
                    return CommandErrorLocalized("role_give_role_exists", args: Format.Bold(target.ToString()));

                await target.AddRolesAsync(roles);
                return roles.Length == 1
                    ? CommandSuccessLocalized("role_give_success",
                        args: new object[] { Format.Bold(target.ToString()), roles[0].Format() })
                    : CommandSuccessLocalized("role_give_success_multiple",
                        args: new object[]
                            {Format.Bold(target.ToString()), string.Join(", ", roles.Select(x => x.Format()))});
            }

            [Command("remove")]
            public async ValueTask<AdminCommandResult> RemoveRoleFromUserAsync(SocketGuildUser target,
                [RequireHierarchy] params SocketRole[] roles)
            {
                if (roles.Length == 0)
                    throw new ArgumentOutOfRangeException(); // TODO: ask quahu how params T[] works

                if (roles.Length == 1 && target.Roles.All(x => x.Id != roles[0].Id))
                    return CommandErrorLocalized("role_remove_role_exists", args: Format.Bold(target.ToString()));

                await target.RemoveRolesAsync(roles);
                return roles.Length == 1
                    ? CommandSuccessLocalized("role_remove_success",
                        args: new object[] { Format.Bold(target.ToString()), roles[0].Format() })
                    : CommandSuccessLocalized("role_remove_success_multiple",
                        args: new object[]
                            {Format.Bold(target.ToString()), string.Join(", ", roles.Select(x => x.Format()))});
            }

            [Command("move")]
            public async ValueTask<AdminCommandResult> MoveRoleAsync([RequireHierarchy] SocketRole target,
                Direction direction, [Remainder] SocketRole move)
            {
                var user = (SocketGuildUser)Context.User;

                if (direction == Direction.Below)
                {
                    if (target.IsEveryone || move.IsEveryone)
                        return CommandErrorLocalized("role_move_below_everyone");

                    if (Context.Guild.CurrentUser.GetHighestRole().Position < move.Position)
                        return CommandErrorLocalized("role_move_below_unable_self",
                            args: new object[] { Format.Bold(move.Name), Format.Bold(target.Name) });
                    if (user.GetHighestRole().Position < move.Position)
                        return CommandErrorLocalized("role_move_below_unable_user",
                            args: new object[] { Format.Bold(move.Name), Format.Bold(target.Name) });

                    await target.ModifyAsync(x => x.Position = move.Position - 1);
                    return CommandSuccessLocalized("role_move_below_success",
                        args: new object[] { target.Format(), move.Format() });
                }

                if (target.IsEveryone)
                    return CommandErrorLocalized("role_move_above_everyone");

                if (Context.Guild.CurrentUser.GetHighestRole().Position < move.Position)
                    return CommandErrorLocalized("role_move_above_unable_self",
                        args: new object[] { Format.Bold(move.Name), Format.Bold(target.Name) });
                if (user.GetHighestRole().Position < move.Position)
                    return CommandErrorLocalized("role_move_above_unable_user",
                        args: new object[] { Format.Bold(move.Name), Format.Bold(target.Name) });

                await target.ModifyAsync(x => x.Position = move.Position + 1);
                return CommandSuccessLocalized("role_move_above_success",
                    args: new object[] { target.Format(), move.Format() });
            }

            [Command("rename")]
            public async ValueTask<AdminCommandResult> RenameRoleAsync([RequireHierarchy] SocketRole target,
                [Remainder] string newName)
            {
                await target.ModifyAsync(x => x.Name = newName);
                return CommandSuccessLocalized("role_rename_success",
                    args: new object[] { target.Format(), Format.Bold(newName) });
            }
        }

        [Command("", "info")]
        public AdminCommandResult GetRoleInfo([Remainder] SocketRole role)
        {
            var color = role.Color == Color.Default
                ? Config.SuccessColor
                : role.Color;

            return CommandSuccess(embed: new EmbedBuilder()
                .WithColor(color)
                .WithTitle(Context.Localize("role_info_title", role.Name))
                .AddField(Context.Localize("info_id"), role.Id, true)
                .AddField(Context.Localize("info_mention"), role.Mention, true)
                .AddField(Context.Localize("role_info_color"),
                    role.Color == Color.Default
                        ? Context.Localize("info_none")
                        : $"[#{color.RawValue:X}](https://www.colorhexa.com/{color.RawValue:X})", true)
                .AddField(Context.Localize("info_position"), FormatPosition())
                .AddField(Context.Localize("info_created"),
                    string.Join('\n', role.CreatedAt.ToString("g", Context.Language.Culture),
                        (DateTimeOffset.UtcNow - role.CreatedAt).HumanizeFormatted(Context, ago: true)), true)
                .AddField(Context.Localize("role_info_permissions"),
                    string.Join('\n',
                        role.Permissions.ToList().Select(x => x.ToString("G").Humanize(LetterCasing.Title))))
                .AddField(Context.Localize("role_info_members", role.Members.Count()), FormatMembers())
                .Build());

            string FormatPosition()
            {
                var builder = new StringBuilder();

                if (role.Position == 0)
                    builder.Append(Context.Localize("role_info_below", 
                        Context.Guild.Roles.Count - role.Position,
                        Context.Guild.Roles.Count));
                else if (role.Position == Context.Guild.Roles.Count - 1)
                    builder.Append(Context.Localize("role_info_above", 
                        Context.Guild.Roles.Count - role.Position,
                        Context.Guild.Roles.Count));
                else
                {
                    var above = Context.Guild.Roles.First(x => x.Position == role.Position + 1);
                    var below = Context.Guild.Roles.First(x => x.Position == role.Position - 1);
                    builder.Append(Context.Localize("role_info_above_below", 
                        Context.Guild.Roles.Count - role.Position,
                        Context.Guild.Roles.Count, 
                        below.Format(), above.Format()));
                }

                return builder.ToString();
            }

            string FormatMembers()
            {
                var builder = new StringBuilder();
                var members = role.Members.OrderByDescending(x => x.JoinedAt ?? DateTimeOffset.UtcNow).ToList();
                for (var i = 0; i < members.Count; i++)
                {
                    var member = members[i];
                    
                    if (builder.Length + member.ToString().Sanitize().Length > 1019) // 1024 - 3
                    {
                        builder.Append('…');
                        break;
                    }

                    builder.Append(member.ToString().Sanitize());

                    if (i != members.Count - 1)
                        builder.Append(", ");
                }
                
                return builder.Length > 0
                    ? builder.ToString()
                    : Context.Localize("info_none");
            }
        }
        
        [Command("color")]
        public AdminCommandResult GetRoleColor([Remainder] SocketRole role)
            => role.Color == Color.Default
                ? CommandSuccessLocalized("role_color_default", args: role.Format())
                : CommandSuccessLocalized("role_color", args: new object[] {role.Format(), $"#{role.Color.RawValue:X}"});
    }
}