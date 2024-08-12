using Disqord.Bot.Hosting;
using Disqord.Utilities.Threading;
using LinqToDB;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Administrator.Bot;

public sealed class DemeritPointDecayService : DiscordBotService
{
    private Cts _cts = new();

    public void CancelCts()
    {
        if (!_cts.IsCancellationRequested)
            _cts.Cancel();
    }
    
#if !MIGRATING
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Bot.WaitUntilReadyAsync(stoppingToken);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);
            var memberWithDecay = await db.Members
                .Where(x => x.LastDemeritPointDecay != null)
                .SelectMany(x => db.Guilds
                    .Where(y => y.GuildId == x.GuildId)
                    .Where(y => y.DemeritPointsDecayInterval != null)
                    .Select(y => new
                    {
                        x.UserId,
                        x.GuildId,
                        x.DemeritPoints,
                        x.LastDemeritPointDecay,
                        y.DemeritPointsDecayInterval
                    }))
                .OrderBy(x => x.LastDemeritPointDecay + x.DemeritPointsDecayInterval!.Value)
                .FirstOrDefaultAsync(cancellationToken: stoppingToken);
            
            /*
            var memberWithDecay = await db.Members.Where(x => db.Guilds
                    .Where(y => y.GuildId == x.GuildId)
                    .Any(y => y.DemeritPointsDecayInterval != null))
                .OrderBy(x => x.LastDemeritPointDecay!.Value)
                .FirstOrDefaultAsync(x => x.LastDemeritPointDecay != null, stoppingToken);
            */

            var delay = memberWithDecay?.LastDemeritPointDecay + memberWithDecay?.DemeritPointsDecayInterval - DateTimeOffset.UtcNow;
            Logger.LogDebug("Demerit point decay delay is {Delay}.", delay);
            /*
            TimeSpan? delay = null;
            if (memberWithDecay?.LastDemeritPointDecay is { } lastDecay)
            {
                var guild = await db.Guilds.GetOrCreateAsync(memberWithDecay.GuildId);
                if (guild.DemeritPointsDecayInterval is { } interval)
                {
                    delay = lastDecay + interval - DateTimeOffset.UtcNow;
                }
            }
            */

            if (delay is null)
            {
                try
                {
                    Logger.LogDebug("No member awaiting demerit point decay - waiting infinitely for CancelCts().");
                    await Task.Delay(-1, _cts.Token);
                }
                catch (TaskCanceledException)
                {
                    Logger.LogDebug("Task.Delay canceled due to CancelCts() being called.");
                    _cts.Dispose();
                    _cts = new();
                    continue;
                }
            }
            else if (delay > TimeSpan.Zero)
            {
                var cts = Cts.Linked(_cts.Token);

                try
                {
                    Logger.LogDebug("Waiting for {Delay} for demerit point decay.", delay);
                    cts.CancelAfter(delay.Value);
                    await Task.Delay(-1, cts.Token);
                }
                catch (TaskCanceledException) when (_cts.IsCancellationRequested)
                {
                    Logger.LogDebug("Task.Delay canceled due to CancelCts() being called.");
                    _cts.Dispose();
                    _cts = new();
                    continue;
                }
                catch (TaskCanceledException)
                {
                    Logger.LogDebug("Task.Delay canceled due to CTS timeout.");
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to run and cancel the delay task.");
                }
                finally
                {
                    cts.Dispose();
                }
            }
            else
            {
                Logger.LogDebug("Timer delay was less than 0 (actual: {Delay}).", delay);
            }

            if (memberWithDecay is null)
                continue;

            await db.Members.Where(x => x.UserId == memberWithDecay.UserId && x.GuildId == memberWithDecay.GuildId)
                .Set(x => x.DemeritPoints, x => Math.Max(0, memberWithDecay.DemeritPoints - 1))
                .Set(x => x.LastDemeritPointDecay, x => memberWithDecay.DemeritPoints > 0
                    ? DateTimeOffset.UtcNow
                    : null)
                .UpdateAsync(stoppingToken);
            
            Logger.LogDebug("Decayed member {MemberId} in guild {GuildId}'s demerit points.",
                memberWithDecay.UserId.RawValue,
                memberWithDecay.GuildId.RawValue);
                

            /*
            memberWithDecay.DemeritPoints = Math.Max(0, memberWithDecay.DemeritPoints - 1);
            memberWithDecay.LastDemeritPointDecay = memberWithDecay.DemeritPoints > 0
                ? DateTimeOffset.UtcNow
                : null;
                */
            
            /*
            var warningWithDemeritPoints = await db.Punishments
                .OfType<Warning>()
                .Where(x => x.GuildId == memberWithDecay.GuildId && x.Target.Id == memberWithDecay.UserId && x.DemeritPointsRemaining > 0)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(stoppingToken);

            if (warningWithDemeritPoints is null)
            {
                // no warnings to decay: REMOVE decay timer
                memberWithDecay.LastDemeritPointDecay = null;
                Logger.LogDebug("Member {MemberId} in guild {GuildId} has no warnings (with demerit points remaining). Removing decay time.",
                    memberWithDecay.UserId.RawValue, memberWithDecay.GuildId.RawValue);
            }
            else
            {
                warningWithDemeritPoints.DemeritPointsRemaining--;
                memberWithDecay.LastDemeritPointDecay = DateTimeOffset.UtcNow;
                Logger.LogDebug("Member {MemberId} in guild {GuildId} has decayed total demerit points by 1. Refreshing decay time.",
                    memberWithDecay.UserId.RawValue, memberWithDecay.GuildId.RawValue);
            }
            */
        }
    }
#endif
}