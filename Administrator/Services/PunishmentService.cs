using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Database;
using Administrator.Extensions;
using Disqord;
using Disqord.AuditLogs;
using Disqord.Bot;
using Disqord.Gateway;
using Disqord.Hosting;
using Disqord.Rest;
using Disqord.Rest.Api;
using Disqord.Rest.Default;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Administrator.Services
{
    public sealed class PunishmentService : DiscordClientService
    {
        private readonly DiscordBotBase _bot;
        private readonly HashSet<Snowflake> _previousBans;
        private readonly HashSet<Snowflake> _previousKicks;
        private readonly HashSet<Snowflake> _previousAuditLogs;

        public PunishmentService(ILogger<PunishmentService> logger, DiscordBotBase bot) 
            : base(logger, bot)
        {
            _bot = bot;
            _previousBans = new HashSet<Snowflake>();
            _previousKicks = new HashSet<Snowflake>();
            _previousAuditLogs = new HashSet<Snowflake>();
        }

        public async Task<PunishmentResult<Ban>> BanAsync(IGuild guild, IUser target, IUser moderator, 
            TimeSpan? duration = null, string reason = null, Upload attachment = null, bool alreadyBanned = false)
        {
            using var scope = _bot.Services.CreateScope();
            await using var ctx = scope.ServiceProvider.GetRequiredService<AdminDbContext>();

            var dbGuild = await ctx.GetOrCreateGuildAsync(guild);

            if (await _bot.FetchBanAsync(guild.Id, target.Id) is not null)
            {
                return new PunishmentResult<Ban>("That user has already been banned!");
            }

            if (!dbGuild.Settings.Has(GuildSetting.Punishments))
            {
                try
                {
                    await guild.CreateBanAsync(target.Id, 
                        $"Moderator: {moderator} | Timestamp: {DateTimeOffset.UtcNow:g} UTC | Reason: {reason ?? "No reason provided."}", 
                        dbGuild.BanPruneDays);
                    _previousBans.Add(target.Id);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "An exception occurred attempting to ban the user {UserId} from the guild {GuildId}.",
                        target.Id.RawValue, guild.Id.RawValue);
                    return new PunishmentResult<Ban>(ex.Message);
                }

                return new PunishmentResult<Ban>((Ban) default);
            }

            var ban = (Ban) ctx.Punishments.Add(Ban.Create(guild, target, moderator, duration, reason, attachment)).Entity;
            await ctx.SaveChangesAsync();

            _ = target.SendMessageAsync(await ban.FormatDmMessageAsync(_bot));

            if (!alreadyBanned)
            {
                try
                {
                    await guild.CreateBanAsync(target.Id, 
                        $"Moderator: {moderator} | Timestamp: {DateTimeOffset.UtcNow:g} UTC | Reason: {reason ?? "No reason provided."}", 
                        dbGuild.BanPruneDays);
                    _previousBans.Add(target.Id);
                }
                catch (Exception ex)
                {
                    ctx.Punishments.Remove(ban);
                    await ctx.SaveChangesAsync();
                
                    Logger.LogError(ex, "An exception occurred attempting to ban the user {UserId} from the guild {GuildId}.",
                        target.Id.RawValue, guild.Id.RawValue);
                    return new PunishmentResult<Ban>(ex.Message);
                }
            }

            if (await ctx.GetLoggingChannelAsync(guild.Id, LoggingChannelType.Ban) is { } channel)
            {
                try
                {
                    var message = await ban.FormatLogMessageAsync(_bot);
                    var logMessage = await _bot.SendMessageAsync(channel.Id, message);
                    ban.SetLogMessage(logMessage);
                    ctx.Punishments.Update(ban);
                    await ctx.SaveChangesAsync();
                }
                catch (RestApiException ex) when (ex.ErrorModel.Code == RestApiErrorCode.MissingPermissions)
                {
                    if (await ctx.GetLoggingChannelAsync(guild.Id, LoggingChannelType.Error) is { } errorChannel)
                    {
                        // TODO: Log error to guild
                    }
                }
            }

            return new PunishmentResult<Ban>(ban);
        }

        public async Task<PunishmentResult<Warning>> WarnAsync(IGuild guild, IUser target, IUser moderator,
            string reason = null, Upload attachment = null)
        {
            using var scope = _bot.Services.CreateScope();
            await using var ctx = scope.ServiceProvider.GetRequiredService<AdminDbContext>();

            var dbGuild = await ctx.GetOrCreateGuildAsync(guild);
            if (!dbGuild.Settings.Has(GuildSetting.Punishments))
            {
                return new PunishmentResult<Warning>("Punishments are not enabled, so warnings will do nothing.");
            }
            
            var warning = (Warning) ctx.Punishments.Add(Warning.Create(guild, target, moderator, reason, attachment)).Entity;
            await ctx.SaveChangesAsync();

            _ = target.SendMessageAsync(await warning.FormatDmMessageAsync(_bot));
            
            if (await ctx.GetLoggingChannelAsync(guild.Id, LoggingChannelType.Warn) is { } channel)
            {
                try
                {
                    var message = await warning.FormatLogMessageAsync(_bot);
                    var logMessage = await _bot.SendMessageAsync(channel.Id, message);
                    warning.SetLogMessage(logMessage);
                    ctx.Punishments.Update(warning);
                    await ctx.SaveChangesAsync();
                }
                catch (RestApiException ex) when (ex.ErrorModel.Code == RestApiErrorCode.MissingPermissions)
                {
                    if (await ctx.GetLoggingChannelAsync(guild.Id, LoggingChannelType.Error) is { } errorChannel)
                    {
                        // TODO: Log error to guild
                    }
                }
            }

            /* TODO: put this in the warn command
            var warnings = await ctx.GetPunishmentsAsync<Warning>(guild.Id, x => x.TargetId == target.Id && !x.IsRevoked);
            if (await ctx.FindAsync<WarningPunishment>(guild.Id, warnings.Count) is { } warningPunishment)
            {
                switch (warningPunishment.Type)
                {
                    case WarningPunishmentType.Mute:
                        var muteResult = await MuteAsync(guild, target, moderator, duration: warningPunishment.Duration,
                            reason: $"Additional punishment from warning #{warning.Id}.");
                        break;
                    case WarningPunishmentType.Kick:
                        var kickResult = await KickAsync(guild, target, moderator, 
                            $"Additional punishment from warning #{warning.Id}.");
                        break;
                    case WarningPunishmentType.Ban:
                        var banResult = await BanAsync(guild, target, moderator, warningPunishment.Duration,
                            $"Additional punishment from warning #{warning.Id}.");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            */

            return new PunishmentResult<Warning>(warning);
        }

        public async Task<PunishmentResult<Kick>> KickAsync(IGuild guild, IUser target, IUser moderator, 
            string reason = null, Upload attachment = null, bool alreadyKicked = false)
        {
            using var scope = _bot.Services.CreateScope();
            await using var ctx = scope.ServiceProvider.GetRequiredService<AdminDbContext>();

            var dbGuild = await ctx.GetOrCreateGuildAsync(guild);

            if (!dbGuild.Settings.Has(GuildSetting.Punishments))
            {
                await guild.KickMemberAsync(target.Id,
                    new DefaultRestRequestOptions
                    {
                        Reason =
                            $"Moderator: {moderator} | Timestamp: {DateTimeOffset.UtcNow:g} UTC | Reason: {reason ?? "No reason provided."}"
                    });

                return new PunishmentResult<Kick>((Kick) default);
            }

            var kick = (Kick) ctx.Punishments.Add(Kick.Create(guild, target, moderator, reason, attachment)).Entity;
            await ctx.SaveChangesAsync();

            _ = target.SendMessageAsync(await kick.FormatDmMessageAsync(_bot));

            if (!alreadyKicked)
            {
                await guild.KickMemberAsync(target.Id, new DefaultRestRequestOptions
                {
                    Reason =
                        $"Moderator: {moderator} | Timestamp: {DateTimeOffset.UtcNow:g} UTC | Reason: {reason ?? "No reason provided."}"
                });

                _previousKicks.Add(target.Id);
            }
            
            if (await ctx.GetLoggingChannelAsync(guild.Id, LoggingChannelType.Kick) is { } channel)
            {
                try
                {
                    var message = await kick.FormatLogMessageAsync(_bot);
                    var logMessage = await _bot.SendMessageAsync(channel.Id, message);
                    kick.SetLogMessage(logMessage);
                    ctx.Punishments.Update(kick);
                    await ctx.SaveChangesAsync();
                }
                catch (RestApiException ex) when (ex.ErrorModel.Code == RestApiErrorCode.MissingPermissions)
                {
                    if (await ctx.GetLoggingChannelAsync(guild.Id, LoggingChannelType.Error) is { } errorChannel)
                    {
                        // TODO: Log error to guild
                    }
                }
            }

            return new PunishmentResult<Kick>(kick);
        }
        
        public async Task<PunishmentResult<Mute>> MuteAsync(IGuild guild, IUser target, IUser moderator, 
            ITextChannel channel = null, TimeSpan? duration = null, 
            string reason = null, Upload attachment = null, bool alreadyMuted = false)
        {
            using var scope = _bot.Services.CreateScope();
            await using var ctx = scope.ServiceProvider.GetRequiredService<AdminDbContext>();

            var muteRole = await ctx.GetSpecialRoleAsync(guild.Id, SpecialRoleType.Mute);
            var dbGuild = await ctx.GetOrCreateGuildAsync(guild);

            if (!dbGuild.Settings.Has(GuildSetting.Punishments))
            {
                if (muteRole is null && channel is null)
                {
                    return new PunishmentResult<Mute>(
                        "Punishments are not enabled and the mute role is not configured, so mutes will do nothing.");
                }
                
                try
                {
                    if (channel is not null)
                    {
                        await channel.SetOverwriteAsync(new LocalOverwrite(target,
                            new OverwritePermissions().Deny(Permission.SendMessages | Permission.AddReactions)));
                    }
                    else
                    {
                        await _bot.GrantRoleAsync(guild.Id, target.Id, muteRole.Id);
                    }
                }
                catch (RestApiException ex) when (ex.ErrorModel.Code == RestApiErrorCode.MissingPermissions)
                {
                    if (await ctx.GetLoggingChannelAsync(guild.Id, LoggingChannelType.Error) is { } errorChannel)
                    {
                        // TODO: Log error to guild
                    }
                }

                return new PunishmentResult<Mute>((Mute) default);
            }

            var mute = (Mute) ctx.Punishments.Add(Mute.Create(guild, target, moderator, channel,
                channel?.Overwrites.FirstOrDefault(x => x.TargetId == target.Id),
                duration, reason, attachment)).Entity;
            await ctx.SaveChangesAsync();

            _ = target.SendMessageAsync(await mute.FormatDmMessageAsync(_bot));

            if (!alreadyMuted)
            {
                try
                {
                    if (channel is not null)
                    {
                        await channel.SetOverwriteAsync(new LocalOverwrite(target,
                            new OverwritePermissions().Deny(Permission.SendMessages | Permission.AddReactions)));
                    }
                    else
                    {
                        await _bot.GrantRoleAsync(guild.Id, target.Id, muteRole.Id);
                    }
                }
                catch (Exception ex)
                {
                    ctx.Punishments.Remove(mute);
                    await ctx.SaveChangesAsync();
                
                    Logger.LogError(ex, "An exception occurred attempting to ban the user {UserId} from the guild {GuildId}.",
                        target.Id.RawValue, guild.Id.RawValue);
                    return new PunishmentResult<Mute>(ex.Message);
                }
            }

            if (await ctx.GetLoggingChannelAsync(guild.Id, LoggingChannelType.Mute) is { } loggingChannel)
            {
                try
                {
                    var message = await mute.FormatLogMessageAsync(_bot);
                    var logMessage = await _bot.SendMessageAsync(loggingChannel.Id, message);
                    mute.SetLogMessage(logMessage);
                    ctx.Punishments.Update(mute);
                    await ctx.SaveChangesAsync();
                }
                catch (RestApiException ex) when (ex.ErrorModel.Code == RestApiErrorCode.MissingPermissions)
                {
                    if (await ctx.GetLoggingChannelAsync(guild.Id, LoggingChannelType.Error) is { } errorChannel)
                    {
                        // TODO: Log error to guild
                    }
                }
            }

            return new PunishmentResult<Mute>(mute);
        }

        public async Task<PunishmentResult<RevocablePunishment>> RevokeAsync(RevocablePunishment punishment, IUser revoker, string reason = null, bool alreadyRevoked = false)
        {
            using var scope = _bot.Services.CreateScope();
            await using var ctx = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
            
            punishment.Revoke(revoker, reason);
            ctx.Punishments.Update(punishment);
            await ctx.SaveChangesAsync();

            _ = Task.Run(async () =>
            {
                var dmChannel = await _bot.CreateDirectChannelAsync(punishment.TargetId);
                await dmChannel.SendMessageAsync(await punishment.FormatRevokedDmMessageAsync(_bot));
            });

            if (await ctx.GetLoggingChannelAsync(punishment.GuildId, punishment switch
            {
                Ban => LoggingChannelType.Unban,
                Mute => LoggingChannelType.Unmute,
                Warning => LoggingChannelType.Unwarn,
                _ => throw new ArgumentOutOfRangeException(nameof(punishment))
            }) is { } loggingChannel)
            {
                try
                {
                    await _bot.SendMessageAsync(loggingChannel.Id,
                        await punishment.FormatRevokedMessageAsync(_bot));
                }
                catch (RestApiException ex) when (ex.ErrorModel.Code == RestApiErrorCode.MissingPermissions)
                {
                    if (await ctx.GetLoggingChannelAsync(punishment.GuildId, LoggingChannelType.Error) is { } errorChannel)
                    {
                        // TODO: Log error to guild
                    }
                }
            }

            if (alreadyRevoked)
                return new PunishmentResult<RevocablePunishment>(punishment);

            if (punishment is Mute mute)
            {
                if (mute.ChannelId.HasValue)
                {
                    try
                    {
                        if (mute.PreviousChannelAllowValue.HasValue)
                        {
                            await _bot.SetOverwriteAsync(mute.ChannelId.Value, new LocalOverwrite(mute.TargetId,
                                OverwriteTargetType.Member,
                                new OverwritePermissions()
                                    .Allow((Permission) mute.PreviousChannelAllowValue.Value)
                                    .Deny((Permission) mute.PreviousChannelDenyValue.Value)));
                        }
                        else
                        {
                            await _bot.DeleteOverwriteAsync(mute.ChannelId.Value, mute.TargetId);
                        }
                    }
                    catch (RestApiException ex) when (ex.ErrorModel.Code == RestApiErrorCode.MissingPermissions)
                    {
                        if (await ctx.GetLoggingChannelAsync(mute.GuildId, LoggingChannelType.Error) is
                            { } errorChannel)
                        {
                            // TODO: Log error to guild
                        }
                    }
                }
                else if (await ctx.FindAsync<SpecialRole>(mute.GuildId, SpecialRoleType.Mute) is { } role)
                {
                    try
                    {
                        await _bot.RevokeRoleAsync(mute.GuildId, mute.TargetId, role.Id);
                    }
                    catch (RestApiException ex) when (ex.ErrorModel.Code == RestApiErrorCode.MissingPermissions)
                    {
                        if (await ctx.GetLoggingChannelAsync(mute.GuildId, LoggingChannelType.Error) is
                            { } errorChannel)
                        {
                            // TODO: Log error to guild
                        }
                    }
                }
                else
                {
                    Logger.LogDebug("Guild {GuildId} doesn't have a mute role set up and mute {MuteId} was not a channel mute.",
                        mute.GuildId.RawValue, mute.Id);
                        
                    // TODO: Log to the guild error channel about this
                }
            }
            else if (punishment is Ban) 
            {
                try
                {
                    await _bot.DeleteBanAsync(punishment.GuildId, punishment.TargetId);
                }
                catch (RestApiException ex) when (ex.ErrorModel.Code == RestApiErrorCode.UnknownBan)
                {
                    Logger.LogDebug("Guild {GuildId} seems to have already unbanned user {UserId}.",
                        punishment.GuildId.RawValue, punishment.TargetId.RawValue);

                    return new PunishmentResult<RevocablePunishment>(
                        "Punishment successfully revoked, but the user has already been unbanned.");
                }
            }

            return new PunishmentResult<RevocablePunishment>(punishment);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (true)
            {
                using var scope = _bot.Services.CreateScope();
                await using var ctx = scope.ServiceProvider.GetRequiredService<AdminDbContext>();

                var punishment = await ctx.Punishments.OfType<RevocablePunishment>()
                    .Where(x => x.ExpiresAt.HasValue && !x.IsExpired)
                    .OrderBy(x => x.ExpiresAt.Value)
                    .FirstOrDefaultAsync(stoppingToken);

                if (punishment is null)
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                    continue;
                }

                var delay = punishment.ExpiresAt.GetValueOrDefault() - DateTimeOffset.UtcNow;

                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, stoppingToken);
                }

                await RevokeAsync(punishment, await _bot.GetOrFetchUserAsync(punishment.ModeratorId),
                    "Punishment expired.");
            }
        }

        protected override async ValueTask OnMemberUpdated(MemberUpdatedEventArgs e)
        {
            if (e.OldMember.RoleIds.Count == e.NewMember.RoleIds.Count ||
                e.NewMember.GetGuild() is not { } guild)
                return;

            var addedRoleIds = e.NewMember.RoleIds.Except(e.OldMember.RoleIds);
            
            using var scope = _bot.Services.CreateScope();
            await using var ctx = scope.ServiceProvider.GetRequiredService<AdminDbContext>();

            var dbGuild = await ctx.GetOrCreateGuildAsync(_bot.GetGuild(e.NewMember.GuildId));
            if (!dbGuild.Settings.Has(GuildSetting.Punishments) ||
                await ctx.GetSpecialRoleAsync(dbGuild.Id, SpecialRoleType.Mute) is not { } muteRole ||
                !addedRoleIds.Contains(muteRole.Id))
            {
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(3)); // wait a few seconds because audit logs are slow and bad

            var logs = await Client.FetchAuditLogsAsync<IMemberRolesUpdatedAuditLog>(dbGuild.Id);
            foreach (var log in logs
                .Where(x => x.TargetId == e.MemberId)
                .OrderByDescending(x => x.Id))
            {
                if (_previousAuditLogs.Contains(log.Id)) continue;
                
                if (log.RolesGranted.HasValue && log.RolesGranted.Value.ContainsKey(muteRole.Id))
                {
                    await MuteAsync(guild, e.NewMember, log.Actor ?? Client.CurrentUser,
                        reason: "Mute role was manually granted.",
                        alreadyMuted: true);
                    
                    _previousAuditLogs.Add(log.Id);
                    break;
                }

                if (log.RolesRevoked.HasValue && log.RolesRevoked.Value.ContainsKey(muteRole.Id))
                {
                    var mutes = await ctx.GetPunishmentsAsync<Mute>(guild.Id,
                        x => !x.IsRevoked && x.TargetId == e.MemberId);

                    if (mutes.OrderByDescending(x => x.Id).FirstOrDefault() is { } muteToRevoke)
                    {
                        await RevokeAsync(muteToRevoke, log.Actor ?? Client.CurrentUser,
                            "Mute role was manually revoked.", true);
                    }
                    
                    _previousAuditLogs.Add(log.Id);
                    break;
                }
            }
        }

        protected override async ValueTask OnMemberLeft(MemberLeftEventArgs e)
        {
            if (_previousKicks.Remove(e.User.Id) ||
                Client.GetGuild(e.GuildId) is not { } guild)
                return;
            
            using var scope = _bot.Services.CreateScope();
            await using var ctx = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
            
            var dbGuild = await ctx.GetOrCreateGuildAsync(_bot.GetGuild(e.GuildId));
            if (!dbGuild.Settings.Has(GuildSetting.Punishments))
                return;
            
            await Task.Delay(TimeSpan.FromSeconds(3)); // wait a few seconds because audit logs are slow and bad

            var logs = await Client.FetchAuditLogsAsync<IMemberKickedAuditLog>(dbGuild.Id);
            foreach (var log in logs
                .Where(x => x.TargetId == e.User.Id)
                .OrderByDescending(x => x.Id))
            {
                if (_previousAuditLogs.Contains(log.Id)) continue;

                await KickAsync(guild, e.User, log.Actor ?? Client.CurrentUser,
                    string.IsNullOrWhiteSpace(log.Reason) ? null : log.Reason, alreadyKicked: true);
                
                _previousAuditLogs.Add(log.Id);
                break;
            }
        }

        protected override async ValueTask OnBanDeleted(BanDeletedEventArgs e)
        {
            using var scope = _bot.Services.CreateScope();
            await using var ctx = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
            
            var dbGuild = await ctx.GetOrCreateGuildAsync(_bot.GetGuild(e.GuildId));
            if (!dbGuild.Settings.Has(GuildSetting.Punishments))
                return;

            var bans = await ctx.GetPunishmentsAsync<Ban>(e.GuildId, x => x.TargetId == e.UserId && !x.IsRevoked);

            if (bans.OrderByDescending(x => x.Id).FirstOrDefault(x => !x.IsRevoked) is not { } banToRevoke)
                return;
            
            await Task.Delay(TimeSpan.FromSeconds(3)); // wait a few seconds because audit logs are slow and bad

            var logs = await Client.FetchAuditLogsAsync<IMemberUnbannedAuditLog>(dbGuild.Id);
            foreach (var log in logs
                .Where(x => x.TargetId == e.User.Id)
                .OrderByDescending(x => x.Id))
            {
                if (_previousAuditLogs.Contains(log.Id)) continue;

                await RevokeAsync(banToRevoke, log.Actor ?? Client.CurrentUser,
                    "Ban was manually revoked.", true);

                _previousAuditLogs.Add(log.Id);
                break;
            }
        }

        protected override async ValueTask OnBanCreated(BanCreatedEventArgs e)
        {
            if (_previousBans.Remove(e.UserId) ||
                Client.GetGuild(e.GuildId) is not { } guild)
                return;
            
            using var scope = _bot.Services.CreateScope();
            await using var ctx = scope.ServiceProvider.GetRequiredService<AdminDbContext>();

            var dbGuild = await ctx.GetOrCreateGuildAsync(_bot.GetGuild(e.GuildId));
            if (!dbGuild.Settings.Has(GuildSetting.Punishments))
                return;
            
            await Task.Delay(TimeSpan.FromSeconds(3)); // wait a few seconds because audit logs are slow and bad

            var logs = await Client.FetchAuditLogsAsync<IMemberBannedAuditLog>(dbGuild.Id);
            foreach (var log in logs
                .Where(x => x.TargetId == e.User.Id)
                .OrderByDescending(x => x.Id))
            {
                if (_previousAuditLogs.Contains(log.Id)) continue;

                await BanAsync(guild, e.User, log.Actor ?? Client.CurrentUser,
                    reason: string.IsNullOrWhiteSpace(log.Reason) ? null : log.Reason, alreadyBanned: true);
                
                _previousAuditLogs.Add(log.Id);
                break;
            }
        }
    }
}