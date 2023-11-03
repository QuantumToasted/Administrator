using System.Text;
using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Disqord.Gateway;
using Humanizer;
using Qmmands;

namespace Administrator.Bot;

public sealed class ModerationModule(PunishmentService punishments) : DiscordApplicationModuleBase
{
    private const int MAX_REASON_LENGTH = Discord.Limits.Rest.MaxAuditLogReasonLength;

    [SlashCommand("ban")]
    [Description("Banishes a user or member from this server.")]
    [RequireGuild]
    [RequireInitialAuthorPermissions(Permissions.BanMembers)]
    [RequireBotPermissions(Permissions.BanMembers)]
    public Task<IResult> BanAsync(
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
            IAttachment? image = null)
    {
        return PunishAsync(() => punishments.BanAsync(Context.GuildId!.Value, target, Context.Author, reason, messagePruneDays, 
            Context.Interaction.CreatedAt() + duration, image));
    }

    [SlashCommand("timeout")]
    [Description("Times out a member in the server, preventing them from chatting and interacting.")]
    [RequireGuild]
    [RequireInitialAuthorPermissions(Permissions.ModerateMembers)]
    [RequireBotPermissions(Permissions.ModerateMembers)]
    public Task<IResult> TimeoutAsync(
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
            IAttachment? image = null)
    {
        return PunishAsync(() => punishments.TimeoutAsync(Context.GuildId!.Value, target, Context.Author, reason, 
            Context.Interaction.CreatedAt() + duration, image));
    }

    [SlashCommand("kick")]
    [Description("Kicks a member from this server.")]
    [RequireGuild]
    [RequireInitialAuthorPermissions(Permissions.KickMembers)]
    [RequireBotPermissions(Permissions.KickMembers)]
    public Task<IResult> KickAsync(
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
            IAttachment? image = null)
    {
        return PunishAsync(() => punishments.KickAsync(Context.GuildId!.Value, target, Context.Author, reason, image));
    }

    [SlashCommand("block")]
    [Description("Blocks a member from a channel, preventing them from chatting or interacting in it.")]
    [RequireGuild]
    [RequireInitialAuthorPermissions(Permissions.ModerateMembers)]
    [RequireBotPermissions(Permissions.ManageChannels)]
    public Task<IResult> BlockAsync(
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
            IAttachment? image = null)
    {
        channel ??= Bot.GetChannel(Context.GuildId!.Value, Context.ChannelId)!;
        return PunishAsync(() => punishments.BlockAsync(Context.GuildId!.Value, target, Context.Author, reason,
            channel, Context.Interaction.CreatedAt() + duration, image));
    }

    [SlashCommand("timed-role-grant")]
    [Description("Grants a member a role as a punishment. Can be temporary or permanent.")]
    public Task<IResult> GrantTimedRoleAsync(
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
        [Description("The reason for granting them this role.")] [Maximum(MAX_REASON_LENGTH)]
            string? reason = null,
        [Description("An image containing context or information for this timed role grant.")]
        [Image]
        [NonNitroAttachment]
            IAttachment? image = null)
    {
        return PunishAsync(() => punishments.GrantTimedRoleAsync(Context.GuildId!.Value, target, Context.Author, reason, role,
            Context.Interaction.CreatedAt() + duration, image));
    }
    
    [SlashCommand("timed-role-revoke")]
    [Description("Revokes a member's role as a punishment. Can be temporary or permanent.")]
    public Task<IResult> TimedRoleRevokeAsync(
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
            IAttachment? image = null)
    {
        return PunishAsync(() => punishments.RevokeTimedRoleAsync(Context.GuildId!.Value, target, Context.Author, reason, role,
            Context.Interaction.CreatedAt() + duration, image));
    }

    [SlashCommand("warn")]
    [Description("Gives a member a warning. (May stack additional punishments.)")]
    [RequireGuild]
    [RequireInitialAuthorPermissions(Permissions.ModerateMembers)]
    [RequireBotPermissions(Permissions.BanMembers | Permissions.ModerateMembers | Permissions.KickMembers)]
    public Task<IResult> WarnAsync(
        [Description("The member to warn.")]
        [RequireNotAuthor]
        [RequireNotBot]
        [RequireAuthorRoleHierarchy]
        [RequireBotRoleHierarchy]
            IMember target,
        [Description("The reason for warning them.")]
        [Maximum(MAX_REASON_LENGTH)]
            string? reason = null,
        [Description("An image containing context or information for this warning.")]
        [Image]
        [NonNitroAttachment]
            IAttachment? image = null)
    {
        return PunishAsync(() => punishments.WarnAsync(Context.GuildId!.Value, target, Context.Author, reason, image));
    }

    [SlashCommand("revoke")]
    [Description("Revokes a specific punishment that has not already been revoked.")]
    [RequireGuild]
    [RequireInitialAuthorPermissions(Permissions.BanMembers)]
    [RequireBotPermissions(Permissions.BanMembers | Permissions.ModerateMembers | Permissions.ManageChannels)]
    public async Task<IResult> RevokeAsync(
        [Description("The ID of the punishment to revoke.")]
            int id,
        [Description("The reason this punishment is being revoked.")]
        [Maximum(MAX_REASON_LENGTH)]
            string? reason = null)
    {
        await Deferral();
        
        var result = await punishments.RevokePunishmentAsync(id, Context.Author, reason, true);
        if (!result.IsSuccessful)
            return Response(result.ErrorMessage);

        var punishment = result.Value;
        var responseBuilder = new StringBuilder($"{punishment.FormatKey()} This {punishment.FormatPunishmentName(LetterCasing.LowerCase)} has been revoked.");
        if (punishment is Warning {AdditionalPunishmentId: { } additionalPunishmentId})
        {
            responseBuilder.AppendNewline()
                .Append($"{Markdown.Code($"[#{additionalPunishmentId}]")} (linked warning punishment) was also automatically revoked.");
        }

        return Response(responseBuilder.ToString());
    }

    [SlashCommand("appeal")]
    [Description("Appeals one of your un-revoked punishments.")]
    public async Task<IResult> AppealAsync(
        [Description("The ID of the punishment you are appealing.")]
            int id,
        [Description("Why you believe the punishment should be revoked.")]
        [Maximum(MAX_REASON_LENGTH)]
            string appeal)
    {
        var result = await punishments.AppealPunishmentAsync(id, appeal);
        if (!result.IsSuccessful)
            return Response(result.ErrorMessage).AsEphemeral();

        return Response($"Your {result.Value.FormatPunishmentName(LetterCasing.LowerCase)} {result.Value.FormatKey()} has had its appeal sent or updated.")
            .AsEphemeral();
    }

    [SlashCommand("reason")]
    [Description("Sets or updates the reason for an existing punishment.")]
    [RequireInitialAuthorPermissions(Permissions.ModerateMembers)]
    [RequireGuild]
    public async Task<IResult> ModifyReasonAsync(
        [Description("The ID of the punishment to modify.")]
            int id,
        [Description("The new or updated reason.")] 
        [Maximum(MAX_REASON_LENGTH)]
            string reason)
    {
        var result = await punishments.UpdateReasonAsync(id, reason);
        if (!result.IsSuccessful)
            return Response(result.ErrorMessage).AsEphemeral();

        return Response($"Modified {result.Value.FormatPunishmentName(LetterCasing.LowerCase)} {result.Value.FormatKey()}'s reason.").AsEphemeral();
    }

    [AutoComplete("reason")]
    public Task AutoCompleteAllPunishmentsAsync(AutoComplete<int> id)
        => id.IsFocused ? punishments.AutoCompletePunishmentsAsync<Punishment>(id) : Task.CompletedTask;

    [AutoComplete("revoke")]
    public Task AutoCompleteRevocablePunishmentsAsync(AutoComplete<int> id)
        => id.IsFocused 
            ? punishments.AutoCompletePunishmentsAsync<RevocablePunishment>(id, static x => !x.RevokedAt.HasValue) 
            : Task.CompletedTask;

    [AutoComplete("appeal")]
    public Task AutoCompleteAppealPunishmentsAsync(AutoComplete<int> id)
        => id.IsFocused
            ? punishments.AutoCompletePunishmentsAsync<RevocablePunishment>(id,
                x => x.Target.Id == Context.AuthorId && !x.RevokedAt.HasValue && x.AppealStatus != AppealStatus.Rejected)
            : Task.CompletedTask;

    private async Task<IResult> PunishAsync<T>(Func<Task<Result<T>>> punishmentFunc)
        where T : Punishment
    {
        await Deferral();
        
        var result = await punishmentFunc.Invoke();
        if (!result.IsSuccessful)
            return Response(result.ErrorMessage)/*.AsEphemeral()*/;

        var response = await result.Value.FormatCommandResponseStringAsync(Bot);
        return Response(response);
    }
}