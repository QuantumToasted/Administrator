using System.Text;
using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Bot.Commands.Interaction;
using Disqord.Gateway;
using Disqord.Rest;
using Humanizer;
using Qmmands;
using Timeout = Administrator.Database.Timeout;

namespace Administrator.Bot;

public sealed partial class ModerationModule(PunishmentService punishments) : DiscordApplicationModuleBase
{
    private const int MAX_REASON_LENGTH = Discord.Limits.Rest.MaxAuditLogReasonLength;

    public partial async Task BanMember(IMember target)
    {
        var modal = GenerateModal<Ban>(target.Id, $"Banning {target.Name}", includeOptionalDuration: true, includeMessagePruneDays: true);
        await Context.Interaction.Response().SendModalAsync(modal);
    }
    
    public partial Task<IResult> Ban(IUser target, TimeSpan? duration = null, string? reason = null, int? messagePruneDays = null, IAttachment? image = null)
    {
        return PunishAsync(Context, () => punishments.BanAsync(Context.GuildId!.Value, target, Context.Author, reason, messagePruneDays, 
            Context.Interaction.CreatedAt() + duration, image));
    }
    
    public partial async Task TimeoutMember(IMember target)
    {
        var modal = GenerateModal<Timeout>(target.Id, $"Timing out {target.Name}", includeRequiredDuration: true);
        await Context.Interaction.Response().SendModalAsync(modal);
    }

    public partial Task<IResult> Timeout(IMember target, TimeSpan duration, string? reason = null, IAttachment? image = null)
    {
        return PunishAsync(Context, () => punishments.TimeoutAsync(Context.GuildId!.Value, target, Context.Author, reason, 
            Context.Interaction.CreatedAt() + duration, image));
    }
    
    public partial async Task KickMember(IMember target)
    {
        var modal = GenerateModal<Kick>(target.Id, $"Kicking {target.Name}");
        await Context.Interaction.Response().SendModalAsync(modal);
    }

    public partial Task<IResult> Kick(IMember target, string? reason = null, IAttachment? image = null)
    {
        return PunishAsync(Context, () => punishments.KickAsync(Context.GuildId!.Value, target, Context.Author, reason, image));
    }

    public partial Task<IResult> Block(IMember target, IChannel? channel = null, TimeSpan? duration = null, string? reason = null, IAttachment? image = null)
    {
        channel ??= Bot.GetChannel(Context.GuildId!.Value, Context.ChannelId)!;
        return PunishAsync(Context, () => punishments.BlockAsync(Context.GuildId!.Value, target, Context.Author, reason,
            channel, Context.Interaction.CreatedAt() + duration, image));
    }

    public partial Task<IResult> TimedRoleGrant(IMember target, IRole role, TimeSpan? duration = null, string? reason = null, IAttachment? image = null)
    {
        return PunishAsync(Context, () => punishments.GrantTimedRoleAsync(Context.GuildId!.Value, target, Context.Author, reason, role,
            Context.Interaction.CreatedAt() + duration, image));
    }
    
    public partial Task<IResult> TimedRoleRevoke(IMember target, IRole role, TimeSpan? duration = null, string? reason = null, IAttachment? image = null)
    {
        return PunishAsync(Context, () => punishments.RevokeTimedRoleAsync(Context.GuildId!.Value, target, Context.Author, reason, role,
            Context.Interaction.CreatedAt() + duration, image));
    }
    
    public partial async Task WarnMember(IMember target)
    {
        var modal = GenerateModal<Warning>(target.Id, $"Warning {target.Name}", includeDemeritPoints: true);
        await Context.Interaction.Response().SendModalAsync(modal);
    }

    public partial Task<IResult> Warn(IMember target, string? reason = null, int? demeritPoints = null, IAttachment? image = null)
    {
        return PunishAsync(Context, () => punishments.WarnAsync(Context.GuildId!.Value, target, Context.Author, reason, demeritPoints, image));
    }

    public partial async Task<IResult> Revoke(int id, string? reason = null)
    {
        await Deferral();
        
        var result = await punishments.RevokePunishmentAsync(Context.GuildId!.Value, id, Context.Author, reason, true);
        if (!result.IsSuccessful)
            return Response(result.ErrorMessage);

        var punishment = result.Value;
        var responseBuilder = new StringBuilder($"{punishment} This {punishment.FormatPunishmentName(LetterCasing.LowerCase)} has been revoked.");

        return Response(responseBuilder.ToString());
    }

    public partial async Task<IResult> Appeal(int id, string appeal)
    {
        var result = await punishments.AppealPunishmentAsync(Context.AuthorId, id, appeal);
        if (!result.IsSuccessful)
            return Response(result.ErrorMessage).AsEphemeral();

        return Response($"Your {result.Value.FormatPunishmentName(LetterCasing.LowerCase)} {result.Value} has had its appeal sent or updated.")
            .AsEphemeral();
    }

    public partial async Task<IResult> ModifyReason(int id, string reason)
    {
        var result = await punishments.UpdateReasonAsync(Context.GuildId!.Value, id, reason);
        if (!result.IsSuccessful)
            return Response(result.ErrorMessage).AsEphemeral();

        return Response($"Modified {result.Value.FormatPunishmentName(LetterCasing.LowerCase)} {result.Value}'s reason.").AsEphemeral();
    }
    
    public partial Task AutoCompleteAllPunishments(AutoComplete<int> id)
        => id.IsFocused ? punishments.AutoCompletePunishmentsAsync<Punishment>(Context.GuildId!.Value, id) : Task.CompletedTask;

    public partial Task AutoCompleteRevocablePunishments(AutoComplete<int> id)
        => id.IsFocused 
            ? punishments.AutoCompletePunishmentsAsync<RevocablePunishment>(Context.GuildId!.Value, id, static x => !x.RevokedAt.HasValue) 
            : Task.CompletedTask;

    public partial Task AutoCompleteAppealPunishments(AutoComplete<int> id)
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