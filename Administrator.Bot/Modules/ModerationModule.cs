using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Qmmands;

namespace Administrator.Bot;

public sealed partial class ModerationModule
{
    [UserCommand("Ban Member")]
    [RequireInitialAuthorPermissions(Permissions.BanMembers)]
    [RequireBotPermissions(Permissions.BanMembers)]
    public partial Task BanMember(
        [RequireNotAuthor]
        [RequireNotBot]
        [RequireHierarchyIfMember]
            IMember target);

    [SlashCommand("ban")]
    [Description("Banishes a user or member from this server.")]
    [RequireGuild]
    [RequireInitialAuthorPermissions(Permissions.BanMembers)]
    [RequireBotPermissions(Permissions.BanMembers)]
    public partial Task<IResult> Ban(
        [Description("The user or member to ban.")]
        [RequireNotAuthor]
        [RequireNotBot]
        [RequireHierarchyIfMember]
            IUser target,
        [Description("The duration to ban them for (example: 1d12h).")]
            TimeSpan? duration = null,
        [Description("The reason for banning them.")]
        [Maximum(MAX_REASON_LENGTH)]
            string? reason = null,
        [Description("The amount of days worth of messages to prune.")]
        [Choice("0 (None)", 0)]
        [Choice("1", 1)]
        [Choice("2", 2)]
        [Choice("3", 3)]
        [Choice("4", 4)]
        [Choice("5", 5)]
        [Choice("6", 6)]
        [Choice("7", 7)]
            int? messagePruneDays = null,
        [Description("An image containing context or information for this ban.")]
        [Image]
        [NonNitroAttachment]
            IAttachment? image = null);

    [UserCommand("Timeout Member")]
    [RequireInitialAuthorPermissions(Permissions.ModerateMembers)]
    [RequireBotPermissions(Permissions.ModerateMembers)]
    public partial Task TimeoutMember(
        [RequireNotAuthor]
        [RequireNotBot]
        [RequireAuthorRoleHierarchy]
        [RequireBotRoleHierarchy]
            IMember target);

    [SlashCommand("timeout")]
    [Description("Times out a member in the server, preventing them from chatting and interacting.")]
    [RequireGuild]
    [RequireInitialAuthorPermissions(Permissions.ModerateMembers)]
    [RequireBotPermissions(Permissions.ModerateMembers)]
    public partial Task<IResult> Timeout(
        [Description("The member to timeout.")]
        [RequireNotAuthor]
        [RequireNotBot]
        [RequireAuthorRoleHierarchy]
        [RequireBotRoleHierarchy]
            IMember target,
        [Description("The duration to time them out for (example: 1d12h).")]
            TimeSpan duration,
        [Description("The reason for giving them a timeout.")]
        [Maximum(MAX_REASON_LENGTH)]
            string? reason = null,
        [Description("An image containing context or information for this timeout.")]
        [Image]
        [NonNitroAttachment]
            IAttachment? image = null);

    [UserCommand("Kick Member")]
    [RequireInitialAuthorPermissions(Permissions.KickMembers)]
    [RequireBotPermissions(Permissions.KickMembers)]
    public partial Task KickMember(
        [RequireNotAuthor]
        [RequireNotBot]
        [RequireAuthorRoleHierarchy]
        [RequireBotRoleHierarchy]
            IMember target);

    [SlashCommand("kick")]
    [Description("Kicks a member from this server.")]
    [RequireGuild]
    [RequireInitialAuthorPermissions(Permissions.KickMembers)]
    [RequireBotPermissions(Permissions.KickMembers)]
    public partial Task<IResult> Kick(
        [Description("The member to kick.")]
        [RequireNotAuthor]
        [RequireNotBot]
        [RequireAuthorRoleHierarchy]
        [RequireBotRoleHierarchy]
            IMember target,
        [Description("The reason for kicking them.")]
        [Maximum(MAX_REASON_LENGTH)]
            string? reason = null,
        [Description("An image containing context or information for this kick.")]
        [Image]
        [NonNitroAttachment]
            IAttachment? image = null);

    [SlashCommand("block")]
    [Description("Blocks a member from a channel, preventing them from chatting or interacting in it.")]
    [RequireGuild]
    [RequireInitialAuthorPermissions(Permissions.ModerateMembers)]
    [RequireBotPermissions(Permissions.ManageChannels)]
    public partial Task<IResult> Block(
        [Description("The member to timeout.")]
        [RequireNotAuthor]
        [RequireNotBot]
        [RequireAuthorRoleHierarchy]
        [RequireBotRoleHierarchy]
            IMember target,
        [Description("The channel to block them from (defaults to the current channel).")]
        [ChannelTypes(ChannelType.Text, ChannelType.Forum)]
        [AuthorCanViewChannel]
        [BotCanViewChannel]
            IChannel? channel = null,
        [Description("The duration to time them out for (example: 1d12h).")]
            TimeSpan? duration = null,
        [Description("The reason for giving them a timeout.")]
        [Maximum(MAX_REASON_LENGTH)]
            string? reason = null,
        [Description("An image containing context or information for this block.")]
        [Image]
        [NonNitroAttachment]
            IAttachment? image = null);

    [SlashCommand("timed-role-grant")]
    [Description("Grants a member a role as a punishment. Can be temporary or permanent.")]
    [RequireInitialAuthorPermissions(Permissions.ManageRoles)]
    public partial Task<IResult> TimedRoleGrant(
        [Description("The member to grant the role to.")]
        [RequireNotAuthor]
        [RequireNotBot]
        [RequireAuthorRoleHierarchy]
        [RequireBotRoleHierarchy]
            IMember target,
        [Description("The role to grant to the member.")]
        [RequireAuthorRoleHierarchy]
        [RequireBotRoleHierarchy]
            IRole role,
        [Description("The duration the role will be granted for (example: 1d12h).")]
            TimeSpan? duration = null,
        [Description("The reason for granting them this role.")]
        [Maximum(MAX_REASON_LENGTH)]
            string? reason = null,
        [Description("An image containing context or information for this timed role grant.")]
        [Image]
        [NonNitroAttachment]
            IAttachment? image = null);

    [SlashCommand("timed-role-revoke")]
    [Description("Revokes a member's role as a punishment. Can be temporary or permanent.")]
    [RequireInitialAuthorPermissions(Permissions.ManageRoles)]
    public partial Task<IResult> TimedRoleRevoke(
        [Description("The member to revoke the role from.")]
        [RequireNotAuthor]
        [RequireNotBot]
        [RequireAuthorRoleHierarchy]
        [RequireBotRoleHierarchy]
            IMember target,
        [Description("The role to revoke from to the member.")]
        [RequireAuthorRoleHierarchy]
        [RequireBotRoleHierarchy]
            IRole role,
        [Description("The duration the role will be revoked for (example: 1d12h).")]
            TimeSpan? duration = null,
        [Description("The reason for revoking this role from them.")]
        [Maximum(MAX_REASON_LENGTH)]
            string? reason = null,
        [Description("An image containing context or information for this timed role revoke.")]
        [Image]
        [NonNitroAttachment]
            IAttachment? image = null);

    [UserCommand("Warn Member")]
    [RequireInitialAuthorPermissions(Permissions.ModerateMembers)]
    [RequireBotPermissions(Permissions.BanMembers | Permissions.ModerateMembers | Permissions.KickMembers)]
    public partial Task WarnMember(
        [RequireNotAuthor]
        [RequireNotBot]
        [RequireAuthorRoleHierarchy]
        [RequireBotRoleHierarchy]
            IMember target);

    [SlashCommand("warn")]
    [Description("Gives a member a warning. (May stack additional punishments.)")]
    [RequireGuild]
    [RequireInitialAuthorPermissions(Permissions.ModerateMembers)]
    [RequireBotPermissions(Permissions.BanMembers | Permissions.ModerateMembers | Permissions.KickMembers)]
    public partial Task<IResult> Warn(
        [Description("The member to warn.")]
        [RequireNotAuthor]
        [RequireNotBot]
        [RequireAuthorRoleHierarchy]
        [RequireBotRoleHierarchy]
            IMember target,
        [Description("The reason for warning them.")]
        [Maximum(MAX_REASON_LENGTH)]
            string? reason = null,
        [Range(0, 50)] [Description("Overrides the number of demerit points for this warning.")]
            int? demeritPoints = null,
        [Description("An image containing context or information for this warning.")]
        [Image]
        [NonNitroAttachment]
            IAttachment? image = null);

    [SlashCommand("revoke")]
    [Description("Revokes a specific punishment that has not already been revoked.")]
    [RequireGuild]
    [RequireInitialAuthorPermissions(Permissions.BanMembers)]
    [RequireBotPermissions(Permissions.BanMembers | Permissions.ModerateMembers | Permissions.ManageChannels)]
    public partial Task<IResult> Revoke(
        [Description("The ID of the punishment to revoke.")]
            int id,
        [Description("The reason this punishment is being revoked.")]
        [Maximum(MAX_REASON_LENGTH)]
            string? reason = null);

    [SlashCommand("appeal")]
    [Description("Appeals one of your un-revoked punishments.")]
    public partial Task<IResult> Appeal(
        [Description("The ID of the punishment you are appealing.")]
            int id,
        [Description("Why you believe the punishment should be revoked.")]
        [Range(50, MAX_REASON_LENGTH)]
            string appeal);

    [SlashCommand("reason")]
    [Description("Sets or updates the reason for an existing punishment.")]
    [RequireInitialAuthorPermissions(Permissions.ModerateMembers)]
    [RequireGuild]
    public partial Task<IResult> ModifyReason(
        [Description("The ID of the punishment to modify.")]
            int id,
        [Description("The new or updated reason.")]
        [Maximum(MAX_REASON_LENGTH)]
            string reason);

    [AutoComplete("reason")]
    public partial Task AutoCompleteAllPunishments(AutoComplete<int> id);

    [AutoComplete("revoke")]
    public partial Task AutoCompleteRevocablePunishments(AutoComplete<int> id);

    [AutoComplete("appeal")]
    public partial Task AutoCompleteAppealPunishments(AutoComplete<int> id);
}