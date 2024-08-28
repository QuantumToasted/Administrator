using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Qmmands;

namespace Administrator.Bot;

[SlashGroup("role")]
[RequireInitialAuthorPermissions(Permissions.ManageRoles)]
[RequireBotPermissions(Permissions.ManageRoles)]
public sealed partial class RoleModule
{
    [SlashCommand("info")]
    [Description("Displays information about a role.")]
    public partial IResult Info(
        [Description("The role to display information for.")]
        IRole role);

    [SlashCommand("mention")]
    [Description("Mentions a role, temporarily making it mentionable if needed.")]
    public partial Task<IResult> Mention(
        [Description("The role to mention.")] [RequireAuthorRoleHierarchy] [RequireBotRoleHierarchy]
        IRole role);

    [SlashCommand("grant")]
    [Description("Grants (gives) a role to a member.")]
    public partial Task<IResult> Grant(
        [Description("The member to give the role to.")]
        IMember member,
        [Description("The role to give to the member.")] [RequireAuthorRoleHierarchy] [RequireBotRoleHierarchy]
        IRole role);

    [SlashCommand("grant-all")]
    [Description("Grants (gives) a role to all members.")]
    public partial Task<IResult> GrantAll(
        [Description("The role to give to the members.")] [RequireAuthorRoleHierarchy] [RequireBotRoleHierarchy]
        IRole roleToGive,
        [Description("Only give role-to-give to members with this role. Defaults to no role (everyone).")]
        IRole? membersWithRole = null);

    [SlashCommand("revoke")]
    [Description("Revokes (removes) a role from a member.")]
    public partial Task<IResult> Revoke(
        [Description("The member to revoke the role from.")]
        IMember member,
        [Description("The role to revoke from the member.")] [RequireAuthorRoleHierarchy] [RequireBotRoleHierarchy]
        IRole role);

    [SlashCommand("revoke-all")]
    [Description("Revokes (removes) a role from all members.")]
    public partial Task<IResult> RevokeAll(
        [Description("The role to revoke from the members.")] [RequireAuthorRoleHierarchy] [RequireBotRoleHierarchy]
        IRole roleToRevoke,
        [Description("Only revoke role-to-revoke from members with this role. Defaults to no role (everyone).")]
        IRole? membersWithRole = null);

    [SlashCommand("create")]
    [Description("Creates a new role.")]
    public partial Task<IResult> Create(
        [Description("The name of the new role.")]
        string name,
        [Description("The color of the new role.")]
        Color? color = null,
        [Description("The role to move the new role above after creation.")] [RequireAuthorRoleHierarchy] [RequireBotRoleHierarchy]
        IRole? aboveRole = null,
        [Description("Whether the new role should be hoisted (visible in the member list). Default: False")]
        bool hoisted = false,
        [Description("Whether the new role should be able to be mentioned by other members. Default: False")]
        bool mentionable = false,
        [Description("The icon for the new role. (Requires the server to have access to this feature.)")] [Image] [NonNitroAttachment]
        IAttachment? icon = null);

    [SlashCommand("clone")]
    [Description("Clones an existing role, including all of its information and permissions.")]
    public partial Task<IResult> Clone(
        [Description("The role to clone.")] [RequireAuthorRoleHierarchy] [RequireBotRoleHierarchy]
        IRole role,
        [Description("The name of the cloned role. Defaults to the name of the original role.")]
        string? name = null);

    [SlashCommand("modify")]
    [Description("Modifies an existing role.")]
    public partial Task Modify(
        [Description("The role to modify.")]
        [RequireAuthorRoleHierarchy]
        [RequireBotRoleHierarchy]
            IRole role);

    [SlashCommand("move")]
    [Description("Moves an existing role above or below another role.")]
    public partial Task<IResult> Move(
        [Description("The role to move above/below target-role.")]
        [RequireAuthorRoleHierarchy]
        [RequireBotRoleHierarchy]
            IRole roleToMove,
        [Description("Whether to move role-to-move above or below target-role.")]
            MoveDirection direction,
        [Description("The role to move role-to-move above/below.")]
        [RequireAuthorRoleHierarchy]
        [RequireBotRoleHierarchy]
            IRole targetRole);

    [SlashCommand("delete")]
    [Description("Deletes an existing role permanently.")]
    public partial Task Delete(
        [Description("The role to delete.")]
        [RequireAuthorRoleHierarchy]
        [RequireBotRoleHierarchy]
            IRole role);
}