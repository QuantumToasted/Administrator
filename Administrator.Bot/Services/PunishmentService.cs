using System.Linq.Expressions;
using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Bot;
using Disqord.Bot.Commands.Application;
using Disqord.Gateway;
using Disqord.Rest;
using Disqord.Rest.Api;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qommon;
using IBan = Administrator.Core.IBan;
using Timeout = Administrator.Database.Timeout;

namespace Administrator.Bot;

[ScopedService]
public sealed class PunishmentService(DiscordBotBase bot, AttachmentService attachments, AdminDbContext db,
    AutoCompleteService autoComplete, PunishmentExpiryService expiryService, ILogger<PunishmentService> logger) : IPunishmentService
{
    // TODO: Make these configurable?
    public const double MINIMUM_APPEAL_WAIT_PERCENTAGE = 0.05;
    public static readonly TimeSpan MinimumAppealPermanentWaitDuration = TimeSpan.FromDays(7);
    
    public async Task AutoCompletePunishmentsAsync<TPunishment>(Snowflake? guildId, AutoComplete<int> punishmentId, Expression<Func<TPunishment, bool>>? predicate = null)
        where TPunishment : Punishment
    {
        var query = db.Punishments.OfType<TPunishment>();

        if (guildId.HasValue) // will only be false in case of `/appeal`...right?
            query = query.Where(x => x.GuildId == guildId.Value);

        if (predicate is not null)
            query = query.Where(predicate);

        var punishments = await query.OrderByDescending(x => x.Id)
            .ToListAsync();
        
        autoComplete.AutoComplete(punishmentId, punishments);
    }
    
    public async Task<Result<Ban>> BanAsync(Snowflake guildId, IUser target, IUser moderator, string? reason, int? messagePruneDays,
        DateTimeOffset? expiresAt, IAttachment? attachment)
    {
        if (await bot.FetchBanAsync(guildId, target.Id) is not null)
            return $"{Markdown.Bold(target)} has already been banned from this server!";

        var guild = await db.Guilds.GetOrCreateAsync(guildId);
        var ban = new Ban(guildId, UserSnapshot.FromUser(target), UserSnapshot.FromUser(moderator), reason, messagePruneDays ?? guild.DefaultBanPruneDays, expiresAt);
        return await ProcessPunishmentAsync(ban, attachment);
    }

    public async Task<Result<Block>> BlockAsync(Snowflake guildId, IUser target, IUser moderator, string? reason, IChannel channel, 
        DateTimeOffset? expiresAt, IAttachment? attachment)
    {
        var textChannel = bot.GetChannel(guildId, channel.Id) as ITextChannel ?? await bot.FetchChannelAsync(channel.Id) as ITextChannel;
        var existingOverwrite = textChannel?.Overwrites.FirstOrDefault(x => x.TargetId == target.Id);
        
        if (existingOverwrite?.Permissions.Denied.HasFlag(Permissions.SendMessages) == true)
            return $"{Markdown.Bold(target)} may already be blocked from this channel! Check the permission overwrites in {Mention.Channel(channel.Id)} first.";
        
        var previousChannelAllowPermissions = existingOverwrite?.Permissions.Allowed;
        var previousChannelDenyPermissions = existingOverwrite?.Permissions.Denied;

        var block = new Block(guildId, UserSnapshot.FromUser(target), UserSnapshot.FromUser(moderator), reason, channel.Id, expiresAt,
            previousChannelAllowPermissions, previousChannelDenyPermissions);

        return await ProcessPunishmentAsync(block, attachment);
    }

    public async Task<Result<Kick>> KickAsync(Snowflake guildId, IUser target, IUser moderator, string? reason, IAttachment? attachment)
    {
        if (await bot.GetOrFetchMemberAsync(guildId, target.Id) is null)
            return $"{Markdown.Bold(target)} is not in this server, or has already been kicked!";
        
        var kick = new Kick(guildId, UserSnapshot.FromUser(target), UserSnapshot.FromUser(moderator), reason);
        return await ProcessPunishmentAsync(kick, attachment);
    }

    public async Task<Result<TimedRole>> GrantTimedRoleAsync(Snowflake guildId, IUser target, IUser moderator, string? reason, IRole role, 
        DateTimeOffset? expiresAt, IAttachment? attachment)
    {
        if (bot.GetMember(guildId, target.Id) is { } member && member.RoleIds.Contains(role.Id))
            return $"{Markdown.Bold(target)} already has the role {role.Mention}!";
        
        var timedRole = new TimedRole(guildId, UserSnapshot.FromUser(target), UserSnapshot.FromUser(moderator), reason, role.Id, 
            TimedRoleApplyMode.Grant, expiresAt);
        
        return await ProcessPunishmentAsync(timedRole, attachment);
    }

    public async Task<Result<TimedRole>> RevokeTimedRoleAsync(Snowflake guildId, IUser target, IUser moderator, string? reason, IRole role, 
        DateTimeOffset? expiresAt, IAttachment? attachment)
    {
        if (bot.GetMember(guildId, target.Id) is { } member && !member.RoleIds.Contains(role.Id))
            return $"{Markdown.Bold(target)} doesn't have the role {role.Mention}!";
        
        var timedRole = new TimedRole(guildId, UserSnapshot.FromUser(target), UserSnapshot.FromUser(moderator), reason, role.Id, 
            TimedRoleApplyMode.Revoke, expiresAt);
        
        return await ProcessPunishmentAsync(timedRole, attachment);
    }

    public async Task<Result<Timeout>> TimeoutAsync(Snowflake guildId, IUser target, IUser moderator, string? reason, DateTimeOffset expiresAt, 
        IAttachment? attachment)
    {
        var timeout = new Timeout(guildId, UserSnapshot.FromUser(target), UserSnapshot.FromUser(moderator), reason, expiresAt);
        return await ProcessPunishmentAsync(timeout, attachment);
    }

    public async Task<Result<Warning>> WarnAsync(Snowflake guildId, IUser target, IUser moderator, string? reason, int? demeritPoints,/* bool decayDemeritPoints,*/ IAttachment? attachment)
    {
        var guild = await db.Guilds.GetOrCreateAsync(guildId);
        demeritPoints ??= guild.DefaultWarningDemeritPoints;
        
        var warning = new Warning(guildId, UserSnapshot.FromUser(target), UserSnapshot.FromUser(moderator), reason, demeritPoints.Value);
        return await ProcessPunishmentAsync(warning, attachment);
    }

    public async Task<Result<Punishment>> UpdateReasonAsync(Snowflake guildId, int punishmentId, string newReason)
    {
        if (await db.Punishments.Include(x => x.Guild).ThenInclude(x => x!.LoggingChannels)
                .FirstOrDefaultAsync(x => x.Id == punishmentId) is not { } punishment || punishment.GuildId != guildId)
        {
            return $"No punishment could be found with the ID {punishmentId}";
        }
        
        punishment.Reason = newReason;

        var logMessage = await punishment.FormatLogMessageAsync<LocalMessage>(bot);
        if (punishment.LogMessageId.HasValue)
        {
            await bot.TryModifyMessageToAsync(punishment.LogChannelId!.Value, punishment.LogMessageId.Value, logMessage);
        }
        else if (await db.LoggingChannels.TryGetLoggingChannelAsync(punishment.GuildId, punishment.GetLogEventType()) is { } logChannel &&
                 await bot.TrySendMessageAsync(logChannel.ChannelId, logMessage) is { } message)
        {
            punishment.LogChannelId = message.ChannelId;
            punishment.LogMessageId = message.Id;
        }

        await db.SaveChangesAsync();
        return punishment;
    }

    public async Task<Result<RevocablePunishment>> AppealPunishmentAsync(Snowflake authorId, int punishmentId, string appeal)
    {
        if (await db.Punishments.Include(x => x.Guild).ThenInclude(x => x!.LoggingChannels)
                .FirstOrDefaultAsync(x => x.Id == punishmentId) is not RevocablePunishment punishment)
        {
            return $"No revocable punishment could be found with the ID {punishmentId}.";
        }

        if (punishment.Target.Id != authorId)
            return $"The punishment {punishment} does not belong to you!";

        if (punishment.RevokedAt.HasValue)
        {
            return $"The punishment {punishment} was already revoked " +
                   $"{Markdown.Timestamp(punishment.RevokedAt.Value, Markdown.TimestampFormat.RelativeTime)}.";
        }
        
        if (punishment.AppealStatus == AppealStatus.Rejected)
        {
            return $"The punishment {punishment}'s appeal has been rejected, and cannot be modified further.";
        }

        if (punishment.AppealStatus is AppealStatus.Sent or AppealStatus.Updated or AppealStatus.Ignored)
        {
            return $"The punishment {punishment}'s appeal has already been sent, " +
                   "and cannot be modified further unless moderators request more information.";
        }

        if (!punishment.CanBeAppealed(out var appealAfter))
        {
            return $"You are trying to appeal too quickly!\n" +
                   $"The punishment {punishment} can be appealed {Markdown.Timestamp(appealAfter.Value, Markdown.TimestampFormat.RelativeTime)}.";
        }
        
        punishment.AppealedAt = DateTimeOffset.UtcNow;
        punishment.AppealText = appeal;

        var logMessage = punishment.FormatAppealLogMessage<LocalMessage>();
        
        if (punishment.AppealMessageId.HasValue)
        {
            await bot.TryModifyMessageToAsync(punishment.AppealChannelId!.Value, punishment.AppealMessageId.Value, logMessage);
        }
        else if (await db.LoggingChannels.FindAsync(punishment.GuildId, LogEventType.Appeal) is { } logChannel &&
                 await bot.TrySendMessageAsync(logChannel.ChannelId, logMessage) is { } message)
        {
            punishment.AppealChannelId = logChannel.ChannelId;
            punishment.AppealMessageId = message.Id;
        }

        punishment.AppealStatus = !punishment.AppealStatus.HasValue
            ? AppealStatus.Sent
            : AppealStatus.Updated;

        await db.SaveChangesAsync();
        return punishment;
    }

    public async Task<Result<RevocablePunishment>> RevokePunishmentAsync(Snowflake guildId, int punishmentId, IUser revoker, string? reason, bool manuallyRevoked)
    {
        if (await db.Punishments.Include(x => x.Guild).ThenInclude(x => x!.LoggingChannels)
                .FirstOrDefaultAsync(x => x.Id == punishmentId) is not RevocablePunishment punishment || punishment.GuildId != guildId)
        {
            return $"No revocable punishment could be found with the ID {punishmentId}.";
        }
        
        if (punishment.RevokedAt.HasValue)
        {
            return $"The punishment {punishment} was already revoked " +
                   $"{Markdown.Timestamp(punishment.RevokedAt.Value, Markdown.TimestampFormat.RelativeTime)}.";
        }

        if (punishment is Timeout timeout)
            timeout.WasManuallyRevoked = manuallyRevoked;
        
        punishment.RevokedAt = DateTimeOffset.UtcNow;
        punishment.RevocationReason = reason;
        punishment.Revoker = UserSnapshot.FromUser(revoker);

        try
        {
            await punishment.RevokeAsync(bot);
        }
        catch (RestApiException ex) when (IsAlreadyRevokedError(ex.ErrorModel))
        { }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to revoke {Type} (#{Id}) from user {UserId} in guild {GuildId}.",
                punishment.GetType().Name, punishment.Id, punishment.Target.Id, punishment.GuildId.RawValue);

            return $"This {punishment.GetType().Name.Humanize(LetterCasing.LowerCase)} was unable to be revoked." +
                   "The following text may be able to help?\n" + Markdown.CodeBlock(ex.Message);
        }

        if (punishment.AppealChannelId.HasValue)
        {
            try
            {
                var avatarUrl = (revoker as IMember)?.GetGuildAvatarUrl(CdnAssetFormat.Automatic) ?? revoker.GetAvatarUrl(CdnAssetFormat.Automatic);
                var message = await bot.FetchMessageAsync(punishment.AppealChannelId.Value, punishment.AppealMessageId!.Value) as IUserMessage;
                await message!.ModifyAsync(x =>
                {
                    x.Embeds = new[]
                    {
                        LocalEmbed.CreateFrom(message!.Embeds[0]).WithHauntedColor()
                            .WithFooter($"Punishment manually revoked by {revoker}", avatarUrl)
                            .WithTimestamp(DateTimeOffset.UtcNow)
                    };

                    x.Components = new List<LocalRowComponent>();
                });
            }
            catch { /* we just want to try to modify it */ }

            punishment.AppealStatus = null;
        }

        if (await db.LoggingChannels.TryGetLoggingChannelAsync(punishment.GuildId, LogEventType.Revoke) is { } logChannel)
        {
            var logMessage = punishment.FormatRevocationLogMessage<LocalMessage>(bot);
            await bot.TrySendMessageAsync(logChannel.ChannelId, logMessage);
        }

        var dmRevokeMessage = punishment.FormatRevocationDmMessage<LocalMessage>(bot);
        if (!punishment.DmChannelId.HasValue)
        {
            if (await bot.TrySendDirectMessageAsync(punishment.Target.Id, dmRevokeMessage) is { } dmMessage)
                punishment.DmChannelId = dmMessage.ChannelId;
        }
        else
        {
            await bot.TrySendMessageAsync(punishment.DmChannelId.Value, dmRevokeMessage);
        }

        if (manuallyRevoked)
        {
            var targetPunishments = await db.Punishments
                .Where(x => x.Target.Id == punishment.Target.Id && x.Id != punishmentId && x.GuildId == guildId)
                .ToListAsync();

            // only reset their demerit point decay if they don't have any other active punishments
            if (targetPunishments.OfType<RevocablePunishment>().All(x => x.RevokedAt.HasValue))
            {
                var member = await db.Members.GetOrCreateAsync(guildId, punishment.Target.Id);
                member.NextDemeritPointDecay = DateTimeOffset.UtcNow;
            }
        }

        await db.SaveChangesAsync();
        return punishment;

        static bool IsAlreadyRevokedError(RestApiErrorJsonModel? model)
        {
            return model?.Code.GetValueOrDefault() is RestApiErrorCode.UnknownRole or RestApiErrorCode.UnknownBan or 
                RestApiErrorCode.UnknownMember or RestApiErrorCode.UnknownOverwrite or RestApiErrorCode.UnknownChannel or 
                RestApiErrorCode.UnknownGuild;
        }
    }
    
    public async Task<Result<TPunishment>> ProcessPunishmentAsync<TPunishment>(TPunishment punishment, IAttachment? attachment, bool alreadyApplied = false)
        where TPunishment : Punishment
    {
        if (attachment is not null && await attachments.GetAttachmentAsync(attachment) is var (stream, fileName))
        {
            var punishmentAttachment = new Attachment(fileName);
            if (await punishmentAttachment.UploadAsync(bot, stream.ToArray()))
            {
                punishment.Attachment = punishmentAttachment;
            }
        }
        
        var guild = await db.Guilds.GetOrCreateAsync(punishment.GuildId);
        await db.Entry(guild)
            .Collection(static x => x.LoggingChannels)
            .LoadAsync();

        db.Punishments.Add(punishment);
        await db.SaveChangesAsync();
        
        // Even though the punishment may fail to apply, might as well DM the user before they are punished, just in case.
        // TODO: store SendMessageAsync result and maybe delete after? bad UX
        var dmMessage = await punishment.FormatDmMessageAsync<LocalMessage>(bot);
        if (await bot.TrySendDirectMessageAsync(punishment.Target.Id, dmMessage) is { } sentMessage)
        {
            punishment.DmChannelId = sentMessage.ChannelId;
            punishment.DmMessageId = sentMessage.Id;
        }
        
        var member = await db.Members.GetOrCreateAsync(punishment.GuildId, punishment.Target.Id);
        var oldDemeritPoints = await db.Punishments.GetCurrentDemeritPointsAsync(punishment.GuildId, punishment.Target.Id);

        try
        {
            if (!alreadyApplied)
                await punishment.ApplyAsync(bot, member);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to apply {Type} to user {UserId} in guild {GuildId}.",
                punishment.GetType().Name, punishment.Target.Id, punishment.GuildId.RawValue);

            return $"This {punishment.GetType().Name.ToLower()} was unable to be applied. " +
                   "The following text may be able to help?\n" + Markdown.CodeBlock(ex.Message);
        }

        var logMessage = await punishment.FormatLogMessageAsync<LocalMessage>(bot);
        if (await db.LoggingChannels.TryGetLoggingChannelAsync(punishment.GuildId, punishment.GetLogEventType()) is { } logChannel &&
            await bot.TrySendMessageAsync(logChannel.ChannelId, logMessage) is { } message)
        {
            punishment.LogChannelId = message.ChannelId;
            punishment.LogMessageId = message.Id;
        }

        if (punishment is Warning warning)
        {
            var newDemeritPoints = oldDemeritPoints + warning.DemeritPoints;
            var automaticPunishments = await db.AutomaticPunishments.Where(x => x.GuildId == punishment.GuildId)
                //.Where(x => x.DemeritPoints >= newDemeritPoints && x.DemeritPoints >= oldDemeritPoints)
                .OrderBy(x => x.DemeritPoints)
                .ToListAsync();
            
            /*
            var demeritPoints = await db.Punishments.OfType<Warning>()
                .Where(x => x.GuildId == warning.GuildId && x.Target.Id == warning.Target.Id)
                .SumAsync(x => x.DemeritPointsRemaining);
            */
            
            if (newDemeritPoints > 0 && newDemeritPoints > oldDemeritPoints && 
                automaticPunishments.FirstOrDefault(x => x.DemeritPoints >= newDemeritPoints && x.DemeritPoints >= oldDemeritPoints) is { } demeritPointPunishment)
            {
                var expiresAt = warning.CreatedAt + demeritPointPunishment.PunishmentDuration;
                Punishment punishmentToApply = demeritPointPunishment.PunishmentType switch
                {
                    PunishmentType.Timeout => new Timeout(warning.GuildId, warning.Target, warning.Moderator,
                        $"Automatic timeout for reaching {Markdown.Bold("demerit point".ToQuantity(demeritPointPunishment.DemeritPoints))}: See case {warning}.", expiresAt!.Value),
                    PunishmentType.Kick => new Kick(warning.GuildId, warning.Target, warning.Moderator,
                        $"Automatic kick for reaching {Markdown.Bold("demerit point".ToQuantity(demeritPointPunishment.DemeritPoints))}: See case {warning}."),
                    PunishmentType.Ban => new Ban(warning.GuildId, warning.Target, warning.Moderator,
                        $"Automatic ban for reaching {Markdown.Bold("demerit point".ToQuantity(demeritPointPunishment.DemeritPoints))}: See case {warning}.", warning.Guild!.DefaultBanPruneDays, expiresAt),
                    _ => throw new ArgumentOutOfRangeException()
                };

                await ProcessPunishmentAsync(punishmentToApply, attachment);
                warning.AdditionalPunishmentId = punishmentToApply.Id;
            }
        }
        
        if (punishment is Ban { ExpiresAt: var newDemeritPointDecayStart } && newDemeritPointDecayStart > member.NextDemeritPointDecay)
        {
            member.NextDemeritPointDecay = newDemeritPointDecayStart;
        }
        
        await db.SaveChangesAsync();
        expiryService.CancelCts();
        return punishment;
    }

    async Task<Result<IBan>> IPunishmentService.BanAsync(Snowflake guildId, IUser target, IUser moderator, string? reason, int? messagePruneDays, DateTimeOffset? expiresAt, IAttachment? attachment)
    {
        var baseResult = await BanAsync(guildId, target, moderator, reason, messagePruneDays, expiresAt, attachment);
        return baseResult.IsSuccessful
            ? baseResult.Value
            : baseResult.ErrorMessage;
    }
    
    async Task<Result<IBlock>> IPunishmentService.BlockAsync(Snowflake guildId, IUser target, IUser moderator, string? reason, IChannel channel, DateTimeOffset? expiresAt, IAttachment? attachment)
    {
        var baseResult = await BlockAsync(guildId, target, moderator, reason, channel, expiresAt, attachment);
        return baseResult.IsSuccessful
            ? baseResult.Value
            : baseResult.ErrorMessage;
    }
    
    async Task<Result<IKick>> IPunishmentService.KickAsync(Snowflake guildId, IUser target, IUser moderator, string? reason, IAttachment? attachment)
    {
        var baseResult = await KickAsync(guildId, target, moderator, reason, attachment);
        return baseResult.IsSuccessful
            ? baseResult.Value
            : baseResult.ErrorMessage;
    }

    async Task<Result<ITimedRole>> IPunishmentService.GrantTimedRoleAsync(Snowflake guildId, IUser target, IUser moderator, string? reason, IRole role, DateTimeOffset? expiresAt, IAttachment? attachment)
    {
        var baseResult = await GrantTimedRoleAsync(guildId, target, moderator, reason, role, expiresAt, attachment);
        return baseResult.IsSuccessful
            ? baseResult.Value
            : baseResult.ErrorMessage;
    }

    async Task<Result<ITimedRole>> IPunishmentService.RevokeTimedRoleAsync(Snowflake guildId, IUser target, IUser moderator, string? reason, IRole role, DateTimeOffset? expiresAt, IAttachment? attachment)
    {
        var baseResult = await RevokeTimedRoleAsync(guildId, target, moderator, reason, role, expiresAt, attachment);
        return baseResult.IsSuccessful
            ? baseResult.Value
            : baseResult.ErrorMessage;
    }

    async Task<Result<ITimeout>> IPunishmentService.TimeoutAsync(Snowflake guildId, IUser target, IUser moderator, string? reason, DateTimeOffset expiresAt, IAttachment? attachment)
    {
        var baseResult = await TimeoutAsync(guildId, target, moderator, reason, expiresAt, attachment);
        return baseResult.IsSuccessful
            ? baseResult.Value
            : baseResult.ErrorMessage;
    }

    async Task<Result<IWarning>> IPunishmentService.WarnAsync(Snowflake guildId, IUser target, IUser moderator, string? reason, int? demeritPoints, IAttachment? attachment)
    {
        var baseResult = await WarnAsync(guildId, target, moderator, reason, demeritPoints, attachment);
        return baseResult.IsSuccessful
            ? baseResult.Value
            : baseResult.ErrorMessage;
    }

    async Task<Result<IRevocablePunishment>> IPunishmentService.RevokePunishmentAsync(Snowflake guildId, int punishmentId, IUser revoker, string? reason)
    {
        var baseResult = await RevokePunishmentAsync(guildId, punishmentId, revoker, reason, true);
        return baseResult.IsSuccessful
            ? baseResult.Value
            : baseResult.ErrorMessage;
    }
}