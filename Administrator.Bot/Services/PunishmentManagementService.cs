using Administrator.Core;
using Administrator.Database;
using Disqord.AuditLogs;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Timeout = Administrator.Database.Timeout;

namespace Administrator.Bot;

public sealed class PunishmentManagementService : DiscordBotService
{
    protected override async ValueTask OnAuditLogCreated(AuditLogCreatedEventArgs e)
    {
        if (!e.AuditLog.ActorId.HasValue || e.AuditLog.ActorId == Bot.CurrentUser.Id || !e.AuditLog.TargetId.HasValue)
            return;
        
        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        var guild = await db.Guilds.GetOrCreateAsync(e.GuildId);
        await db.Guilds.Entry(guild)
            .Collection(x => x.Punishments)
            .LoadAsync();

        if (!guild.HasSetting(GuildSettings.AutomaticPunishmentDetection))
            return;

        var punishments = scope.ServiceProvider.GetRequiredService<PunishmentService>();
        var moderator = e.AuditLog.Actor ??
                    await Bot.GetOrFetchUserAsync(e.AuditLog.ActorId.Value) ??
                    Bot.CurrentUser;
        
        if (await Bot.GetOrFetchUserAsync(e.AuditLog.TargetId.Value) is not { } target)
            return;
        
        var reason = e.AuditLog.Reason ?? "No reason provided (via audit logs).";
        switch (e.AuditLog)
        {
            case IMemberBannedAuditLog:
            {
                await punishments.ProcessPunishmentAsync(new Ban(e.GuildId, UserSnapshot.FromUser(target), UserSnapshot.FromUser(moderator), reason, null, null),
                    null, true);
                return;
            }
            case IMemberUnbannedAuditLog when guild.Punishments!.OfType<Ban>()
                .FirstOrDefault(x => x.Target.Id == target.Id && !x.RevokedAt.HasValue) is { } ban:
            {
                await punishments.RevokePunishmentAsync(e.GuildId, ban.Id, moderator, reason, true);
                return;
            }
            case IMemberUpdatedAuditLog { Changes.TimedOutUntil: {WasChanged: true} change }:
            {
                // no timeout -> timeout
                if (!change.OldValue.HasValue && change.NewValue.HasValue)
                {
                    await punishments.ProcessPunishmentAsync(
                        new Timeout(e.GuildId, UserSnapshot.FromUser(target), UserSnapshot.FromUser(moderator), reason, change.NewValue.Value), null, true);
                }
                // timeout -> no timeout
                else if (change.OldValue.HasValue && !change.NewValue.HasValue && guild.Punishments!.OfType<Timeout>()
                         .FirstOrDefault(x => x.Target.Id == target.Id && !x.RevokedAt.HasValue) is { } timeout)
                {
                    await punishments.RevokePunishmentAsync(e.GuildId, timeout.Id, moderator, reason, true);
                }
                
                return;
            }
            case IMemberKickedAuditLog:
            {
                await punishments.ProcessPunishmentAsync(new Kick(e.GuildId, UserSnapshot.FromUser(target), UserSnapshot.FromUser(moderator), reason), null, true);
                return;
            }
        }
    }

    protected override async ValueTask OnMemberJoined(MemberJoinedEventArgs e)
    {
        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        var punishments = await db.Punishments
            .Where(x => x.GuildId == e.GuildId && x.Target.Id == e.MemberId.RawValue)
            .ToListAsync();
            
        var punishmentsToApply = punishments.OfType<RevocablePunishment>()
            .Where(x => !x.RevokedAt.HasValue && x is not Ban)
            .ToList();

        if (punishmentsToApply.Count == 0)
            return;

        var member = await db.Members.GetOrCreateAsync(e.GuildId, e.MemberId);
        foreach (var punishment in punishmentsToApply)
        {
            try
            {
                await punishment.ApplyAsync(Bot, member);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to re-apply punishment to rejoining member {MemberId} in guild {GuildId}.",
                    e.MemberId.RawValue, e.GuildId.RawValue);
            }
        }
    }
}