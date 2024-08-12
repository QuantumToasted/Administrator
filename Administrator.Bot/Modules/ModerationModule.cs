using System.Text;
using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Disqord.Bot.Commands.Interaction;
using Disqord.Gateway;
using Disqord.Rest;
using Humanizer;
using Qmmands;
using Timeout = Administrator.Database.Timeout;

namespace Administrator.Bot;

public sealed class ModerationModule(PunishmentService punishments) : DiscordApplicationModuleBase
{
    private const int MAX_REASON_LENGTH = Discord.Limits.Rest.MaxAuditLogReasonLength;

    [UserCommand("Ban Member")]
    [RequireInitialAuthorPermissions(Permissions.BanMembers)]
    [RequireBotPermissions(Permissions.BanMembers)]
    public async Task BanAsync(
        [RequireNotAuthor]
        [RequireNotBot]
        [RequireHierarchyIfMember]
            IMember target)
    {
        var modal = GenerateModal<Ban>(target.Id, $"Banning {target.Name}", includeOptionalDuration: true, includeMessagePruneDays: true);
        await Context.Interaction.Response().SendModalAsync(modal);
    }
    
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
        return PunishAsync(Context, () => punishments.BanAsync(Context.GuildId!.Value, target, Context.Author, reason, messagePruneDays, 
            Context.Interaction.CreatedAt() + duration, image));
    }
    
    [UserCommand("Timeout Member")]
    [RequireInitialAuthorPermissions(Permissions.ModerateMembers)]
    [RequireBotPermissions(Permissions.ModerateMembers)]
    public async Task TimeoutMemberAsync(
        [RequireNotAuthor]
        [RequireNotBot]
        [RequireAuthorRoleHierarchy]
        [RequireBotRoleHierarchy]
            IMember target)
    {
        var modal = GenerateModal<Timeout>(target.Id, $"Timing out {target.Name}", includeRequiredDuration: true);
        await Context.Interaction.Response().SendModalAsync(modal);
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
        return PunishAsync(Context, () => punishments.TimeoutAsync(Context.GuildId!.Value, target, Context.Author, reason, 
            Context.Interaction.CreatedAt() + duration, image));
    }
    
    [UserCommand("Kick Member")]
    [RequireInitialAuthorPermissions(Permissions.KickMembers)]
    [RequireBotPermissions(Permissions.KickMembers)]
    public async Task KickMemberAsync(
        [RequireNotAuthor]
        [RequireNotBot]
        [RequireAuthorRoleHierarchy]
        [RequireBotRoleHierarchy]
            IMember target)
    {
        var modal = GenerateModal<Kick>(target.Id, $"Kicking {target.Name}");
        await Context.Interaction.Response().SendModalAsync(modal);
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
        return PunishAsync(Context, () => punishments.KickAsync(Context.GuildId!.Value, target, Context.Author, reason, image));
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
        return PunishAsync(Context, () => punishments.BlockAsync(Context.GuildId!.Value, target, Context.Author, reason,
            channel, Context.Interaction.CreatedAt() + duration, image));
    }

    [SlashCommand("timed-role-grant")]
    [Description("Grants a member a role as a punishment. Can be temporary or permanent.")]
    [RequireInitialAuthorPermissions(Permissions.ManageRoles)]
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
        return PunishAsync(Context, () => punishments.GrantTimedRoleAsync(Context.GuildId!.Value, target, Context.Author, reason, role,
            Context.Interaction.CreatedAt() + duration, image));
    }
    
    [SlashCommand("timed-role-revoke")]
    [Description("Revokes a member's role as a punishment. Can be temporary or permanent.")]
    [RequireInitialAuthorPermissions(Permissions.ManageRoles)]
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
        return PunishAsync(Context, () => punishments.RevokeTimedRoleAsync(Context.GuildId!.Value, target, Context.Author, reason, role,
            Context.Interaction.CreatedAt() + duration, image));
    }
    
    [UserCommand("Warn Member")]
    [RequireInitialAuthorPermissions(Permissions.ModerateMembers)]
    [RequireBotPermissions(Permissions.BanMembers | Permissions.ModerateMembers | Permissions.KickMembers)]
    public async Task WarnMemberAsync(
        [RequireNotAuthor]
        [RequireNotBot]
        [RequireAuthorRoleHierarchy]
        [RequireBotRoleHierarchy]
            IMember target)
    {
        var modal = GenerateModal<Warning>(target.Id, $"Warning {target.Name}", includeDemeritPoints: true);
        await Context.Interaction.Response().SendModalAsync(modal);
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
        [Range(0, 50)]
        [Description("Overrides the number of demerit points for this warning.")]
            int? demeritPoints = null,
        /*
        [Description("Whether this warning's demerit points should decay over time.")]
            bool decayDemeritPoints = true,
            */
        [Description("An image containing context or information for this warning.")]
        [Image]
        [NonNitroAttachment]
            IAttachment? image = null)
    {
        return PunishAsync(Context, () => punishments.WarnAsync(Context.GuildId!.Value, target, Context.Author, reason, demeritPoints,/* decayDemeritPoints,*/ image));
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
        
        var result = await punishments.RevokePunishmentAsync(Context.GuildId!.Value, id, Context.Author, reason, true);
        if (!result.IsSuccessful)
            return Response(result.ErrorMessage);

        var punishment = result.Value;
        var responseBuilder = new StringBuilder($"{punishment} This {punishment.FormatPunishmentName(LetterCasing.LowerCase)} has been revoked.");

        return Response(responseBuilder.ToString());
    }

    [SlashCommand("appeal")]
    [Description("Appeals one of your un-revoked punishments.")]
    public async Task<IResult> AppealAsync(
        [Description("The ID of the punishment you are appealing.")]
            int id,
        [Description("Why you believe the punishment should be revoked.")]
        [Range(50, MAX_REASON_LENGTH)]
            string appeal)
    {
        var result = await punishments.AppealPunishmentAsync(Context.AuthorId, id, appeal);
        if (!result.IsSuccessful)
            return Response(result.ErrorMessage).AsEphemeral();

        return Response($"Your {result.Value.FormatPunishmentName(LetterCasing.LowerCase)} {result.Value} has had its appeal sent or updated.")
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
        var result = await punishments.UpdateReasonAsync(Context.GuildId!.Value, id, reason);
        if (!result.IsSuccessful)
            return Response(result.ErrorMessage).AsEphemeral();

        return Response($"Modified {result.Value.FormatPunishmentName(LetterCasing.LowerCase)} {result.Value}'s reason.").AsEphemeral();
    }
    
    [AutoComplete("reason")]
    public Task AutoCompleteAllPunishmentsAsync(AutoComplete<int> id)
        => id.IsFocused ? punishments.AutoCompletePunishmentsAsync<Punishment>(Context.GuildId!.Value, id) : Task.CompletedTask;

    [AutoComplete("revoke")]
    public Task AutoCompleteRevocablePunishmentsAsync(AutoComplete<int> id)
        => id.IsFocused 
            ? punishments.AutoCompletePunishmentsAsync<RevocablePunishment>(Context.GuildId!.Value, id, static x => !x.RevokedAt.HasValue) 
            : Task.CompletedTask;

    [AutoComplete("appeal")]
    public Task AutoCompleteAppealPunishmentsAsync(AutoComplete<int> id)
        => id.IsFocused
            ? punishments.AutoCompletePunishmentsAsync<RevocablePunishment>(Context.GuildId, id,
                x => x.Target.Id == Context.AuthorId.RawValue && !x.RevokedAt.HasValue && x.AppealStatus != AppealStatus.Rejected)
            : Task.CompletedTask;

    public static async Task<IResult> PunishAsync<T>(IDiscordInteractionCommandContext context, Func<Task<Result<T>>> punishmentFunc)
        where T : Punishment
    {
        await context.Interaction.Response().DeferAsync();
        //await Deferral();
        
        var result = await punishmentFunc.Invoke();
        if (!result.IsSuccessful)
            return new DiscordInteractionResponseCommandResult(context, new LocalInteractionMessageResponse().WithContent(result.ErrorMessage));

        var response = await result.Value.FormatCommandResponseStringAsync(context.Bot);
        return new DiscordInteractionResponseCommandResult(context, new LocalInteractionMessageResponse().WithContent(response));
    }

    private static LocalInteractionModalResponse GenerateModal<TPunishment>(Snowflake userId, string title,
        bool includeDemeritPoints = false,
        bool includeOptionalDuration = false,
        bool includeRequiredDuration = false,
        bool includeMessagePruneDays = false) where TPunishment : Punishment
    {
        var modal = new LocalInteractionModalResponse()
            .WithCustomId($"{typeof(TPunishment).Name}:{userId}")
            .WithTitle(title);

        var reasonRow = new LocalRowComponent()
            .AddComponent(new LocalTextInputComponent()
                .WithCustomId("reason")
                .WithStyle(TextInputComponentStyle.Paragraph)
                .WithLabel("Reason")
                .WithIsRequired(false)
                .WithMaximumInputLength(MAX_REASON_LENGTH));
        
        modal.AddComponent(reasonRow);

        if (includeDemeritPoints)
        {
            var demeritPointsRow = new LocalRowComponent()
                .AddComponent(new LocalTextInputComponent()
                    .WithCustomId("demeritPoints")
                    .WithStyle(TextInputComponentStyle.Short)
                    .WithLabel("Demerit Points")
                    .WithIsRequired(false)
                    .WithMaximumInputLength(5));
            
            modal.AddComponent(demeritPointsRow);
        }

        if (includeOptionalDuration || includeRequiredDuration)
        {
            var durationRow = new LocalRowComponent()
                .AddComponent(new LocalTextInputComponent()
                    .WithCustomId("duration")
                    .WithStyle(TextInputComponentStyle.Short)
                    .WithLabel("Duration")
                    .WithIsRequired(includeRequiredDuration)
                    .WithPlaceholder(includeRequiredDuration
                        ? "Example: 1h"
                        : "Leave blank for permanent")
                    .WithMaximumInputLength(20));
            
            modal.AddComponent(durationRow);
        }

        if (includeMessagePruneDays)
        {
            var messagePruneDaysRow = new LocalRowComponent()
                .AddComponent(new LocalTextInputComponent()
                    .WithCustomId("messagePruneDays")
                    .WithStyle(TextInputComponentStyle.Short)
                    .WithLabel("Prune Messages")
                    .WithIsRequired(false)
                    .WithPlaceholder("Number of days to prune messages (0-7)")
                    .WithMaximumInputLength(1));
            
            modal.AddComponent(messagePruneDaysRow);
        }
        
        return modal;
    }
}