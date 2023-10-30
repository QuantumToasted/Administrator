using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Bot;
using Disqord.Bot.Commands;
using Disqord.Gateway;
using Disqord.Rest;
using Disqord.Rest.Api;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qommon;
using Timeout = Administrator.Database.Timeout;

namespace Administrator.Bot;

[ScopedService]
public sealed class PunishmentService(DiscordBotBase bot, AttachmentService attachments, AdminDbContext db, ICommandContextAccessor contextAccessor,
    ILogger<PunishmentService> logger)
{
    private readonly IDiscordCommandContext _context = contextAccessor.Context;
    
    public async Task<Result<Ban>> BanAsync(Snowflake guildId, IUser target, IUser moderator, string? reason, int? messagePruneDays,
        DateTimeOffset? expiresAt, IAttachment? attachment)
    {
        if (await bot.FetchBanAsync(guildId, target.Id) is not null)
            return $"{target} has already been banned from this server!";
        
        var ban = new Ban(guildId, target.Id, target.Name, moderator.Id, moderator.Name, reason, messagePruneDays, expiresAt);
        return await ProcessPunishmentAsync(ban, attachment);
    }

    public async Task<Result<Block>> BlockAsync(Snowflake guildId, IUser target, IUser moderator, string? reason, IGuildChannel channel, 
        DateTimeOffset? expiresAt, IAttachment attachment)
    {
        var textChannel = bot.GetChannel(guildId, channel.Id) as ITextChannel ?? await bot.FetchChannelAsync(channel.Id) as ITextChannel;
        if (textChannel?.Overwrites.Any(x => x.TargetId == target.Id && x.Permissions.Denied.HasFlag(Permissions.SendMessages)) == true)
            return $"{target} may already be blocked from this channel! Check the permission overwrites in {Mention.Channel(channel.Id)} first.";
        
        var overwrite = channel.Overwrites.FirstOrDefault(x => x.TargetId == target.Id);
        var previousChannelAllowPermissions = overwrite?.Permissions.Allowed;
        var previousChannelDenyPermissions = overwrite?.Permissions.Denied;

        var block = new Block(guildId, target.Id, target.Name, moderator.Id, moderator.Name, reason, channel.Id, expiresAt,
            previousChannelAllowPermissions, previousChannelDenyPermissions);

        return await ProcessPunishmentAsync(block, attachment);
    }

    public async Task<Result<Kick>> KickAsync(Snowflake guildId, IUser target, IUser moderator, string? reason, IAttachment? attachment)
    {
        if (await bot.GetOrFetchMemberAsync(guildId, target.Id) is null)
            return $"{target} is not in this server, or has already been kicked!";
        
        var kick = new Kick(guildId, target.Id, target.Name, moderator.Id, moderator.Name, reason);
        return await ProcessPunishmentAsync(kick, attachment);
    }

    public async Task<Result<TimedRole>> GrantTimedRoleAsync(Snowflake guildId, IUser target, IUser moderator, string? reason, IRole role, 
        DateTimeOffset? expiresAt, IAttachment? attachment)
    {
        if (bot.GetMember(guildId, target.Id) is { } member && member.RoleIds.Contains(role.Id))
            return $"{target} already has the role {role.Mention}!";
        
        var timedRole = new TimedRole(guildId, target.Id, target.Name, moderator.Id, moderator.Name, reason, role.Id, 
            TimedRoleApplyMode.Grant, expiresAt);
        
        return await ProcessPunishmentAsync(timedRole, attachment);
    }

    public async Task<Result<TimedRole>> RevokeTimedRoleAsync(Snowflake guildId, IUser target, IUser moderator, string? reason, IRole role, 
        DateTimeOffset? expiresAt, IAttachment? attachment)
    {
        if (bot.GetMember(guildId, target.Id) is { } member && !member.RoleIds.Contains(role.Id))
            return $"{target} doesn't have the role {role.Mention}!";
        
        var timedRole = new TimedRole(guildId, target.Id, target.Name, moderator.Id, moderator.Name, reason, role.Id, 
            TimedRoleApplyMode.Revoke, expiresAt);
        
        return await ProcessPunishmentAsync(timedRole, attachment);
    }

    public async Task<Result<Timeout>> TimeoutAsync(Snowflake guildId, IUser target, IUser moderator, string? reason, DateTimeOffset expiresAt, 
        IAttachment? attachment)
    {
        var timeout = new Timeout(guildId, target.Id, target.Name, moderator.Id, moderator.Name, reason, expiresAt);
        return await ProcessPunishmentAsync(timeout, attachment);
    }

    public Task<Result<Warning>> WarnAsync(Snowflake guildId, IUser target, IUser moderator, string? reason, IAttachment? attachment)
    {
        var warning = new Warning(guildId, target.Id, target.Name, moderator.Id, moderator.Name, reason);
        return ProcessPunishmentAsync(warning, attachment);
    }
    
    public async Task<Result<TPunishment>> ProcessPunishmentAsync<TPunishment>(TPunishment punishment, IAttachment? attachment, bool alreadyApplied = false)
        where TPunishment : Punishment
    {
        if (attachment is not null && await attachments.GetAttachmentAsync(attachment) is var (stream, fileName))
            punishment.Attachment = new Attachment(stream.ToArray(), fileName);
        
        var guild = await db.GetOrCreateGuildConfigAsync(punishment.GuildId);
        await db.Entry(guild)
            .Collection(static x => x.LoggingChannels!)
            .LoadAsync();

        db.Punishments.Add(punishment);
        await db.SaveChangesAsync();
        
        // Even though the punishment may fail to apply, might as well DM the user before they are punished, just in case.
        // TODO: store SendMessageAsync result and maybe delete after? bad UX
        var dmMessage = punishment.FormatDmMessage<LocalMessage>(bot);
        if (await bot.TrySendDirectMessageAsync(punishment.TargetId, dmMessage) is { } sentMessage)
        {
            punishment.DmChannelId = sentMessage.ChannelId;
            punishment.DmMessageId = sentMessage.Id;
        }

        if (!alreadyApplied)
        {
            try
            {
                await punishment.ApplyAsync(bot);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unable to apply {Type} to user {UserId} in guild {GuildId}.",
                    punishment.GetType().Name, punishment.TargetId.RawValue, punishment.GuildId.RawValue);

                return $"This {punishment.GetType().Name.ToLower()} was unable to be applied. " +
                       "The following text may be able to help?\n" + Markdown.CodeBlock(ex.Message);
            }
        }

        if (guild.GetLoggingChannel(punishment.GetLogEventType()) is { } logChannel &&
            await bot.TrySendMessageAsync(logChannel.ChannelId, punishment.FormatLogMessage<LocalMessage>(bot)) is { } message)
        {
            punishment.LogChannelId = message.ChannelId;
            punishment.LogMessageId = message.Id;
        }

        await db.SaveChangesAsync();
        return punishment;
    }

    public async Task<Result<Punishment>> UpdateReasonAsync(int punishmentId, string newReason)
    {
        if (await db.Punishments.Include(x => x.Guild).ThenInclude(x => x!.LoggingChannels)
                .FirstOrDefaultAsync(x => x.Id == punishmentId) is not { } punishment || punishment.GuildId != _context.GuildId!.Value)
        {
            return $"No punishment could be found with the ID {punishmentId}";
        }
        
        punishment.Reason = newReason;

        var logMessage = punishment.FormatLogMessage<LocalMessage>(bot);
        if (punishment.LogMessageId.HasValue)
        {
            await bot.TryModifyMessageToAsync(punishment.LogChannelId!.Value, punishment.LogMessageId.Value, logMessage);
        }
        else if (punishment.Guild!.GetLoggingChannel(punishment.GetLogEventType()) is { } logChannel &&
                 await bot.TrySendMessageAsync(logChannel.ChannelId, logMessage) is { } message)
        {
            punishment.LogChannelId = message.ChannelId;
            punishment.LogMessageId = message.Id;
        }

        await db.SaveChangesAsync();
        return punishment;
    }

    public async Task<Result<RevocablePunishment>> AppealPunishmentAsync(int punishmentId, string appeal)
    {
        if (await db.Punishments.Include(x => x.Guild).ThenInclude(x => x!.LoggingChannels)
                .FirstOrDefaultAsync(x => x.Id == punishmentId) is not RevocablePunishment punishment)
        {
            return $"No revocable punishment could be found with the ID {punishmentId}.";
        }

        if (punishment.TargetId != _context.AuthorId)
            return $"The punishment {punishment} does not belong to you!";

        if (punishment.RevokedAt.HasValue)
        {
            return $"The punishment {punishment} was already revoked " +
                   Markdown.Timestamp(punishment.RevokedAt.Value, Markdown.TimestampFormat.RelativeTime);
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

    public async Task<Result<RevocablePunishment>> RevokePunishmentAsync(int punishmentId, IUser revoker, string? reason, bool alreadyRevoked)
    {
        if (await db.Punishments.Include(x => x.Guild).ThenInclude(x => x!.LoggingChannels)
                .FirstOrDefaultAsync(x => x.Id == punishmentId) is not RevocablePunishment punishment ||
            punishment.GuildId != _context.GuildId!.Value)
        {
            return $"No revocable punishment could be found with the ID {punishmentId}";
        }
        
        if (punishment.RevokedAt.HasValue)
        {
            return $"The punishment {punishment} was already revoked " +
                   $"{Markdown.Timestamp(punishment.RevokedAt.Value, Markdown.TimestampFormat.RelativeTime)}.";
        }

        if (punishment is Timeout timeout)
            timeout.WasManuallyRevoked = alreadyRevoked;

        try
        {
            await punishment.RevokeAsync(bot);
        }
        catch (RestApiException ex) when (IsAlreadyRevokedError(ex.ErrorModel))
        { }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to revoke {Type} (#{Id}) from user {UserId} in guild {GuildId}.",
                punishment.GetType().Name, punishment.Id, punishment.TargetId.RawValue, punishment.GuildId.RawValue);

            return $"This {punishment.GetType().Name.ToLower()} was unable to be revoked." +
                   "The following text may be able to help?\n" + Markdown.CodeBlock(ex.Message);
        }

        punishment.RevokedAt = DateTimeOffset.UtcNow;
        punishment.RevocationReason = reason;
        punishment.RevokerId = revoker.Id;
        punishment.RevokerName = revoker.Name;

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
                });
            }
            catch { /* we just want to try to modify it */ }
        }

        if (punishment.Guild!.GetLoggingChannel(LogEventType.Revoke) is { } logChannel)
        {
            var logMessage = punishment.FormatRevocationLogMessage<LocalMessage>(bot);
            await bot.TrySendMessageAsync(logChannel.ChannelId, logMessage);
        }

        var dmRevokeMessage = punishment.FormatRevocationDmMessage<LocalMessage>(bot);
        if (!punishment.DmChannelId.HasValue)
        {
            if (await bot.TrySendDirectMessageAsync(punishment.TargetId, dmRevokeMessage) is { } dmMessage)
                punishment.DmChannelId = dmMessage.ChannelId;
        }
        else
        {
            await bot.TrySendMessageAsync(punishment.DmChannelId.Value, dmRevokeMessage);
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
}