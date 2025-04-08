using Disqord.Bot.Hosting;
using Disqord.Utilities.Threading;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Administrator.Bot;

#if NOJOBS
public sealed class ReminderExpiryService : DiscordBotService
{
    private const int MAX_FAILURE_COUNT = 5;
    private static readonly TimeSpan MaxDelay = TimeSpan.FromMilliseconds(uint.MaxValue - 1);
    
    private Cts _cts = new();
    
    public void CancelCts()
    {
        if (!_cts.IsCancellationRequested)
            _cts.Cancel();

        //_cts = new();
    }
    
//#if !MIGRATING
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Bot.WaitUntilReadyAsync(stoppingToken);
        
        var failureCount = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);

            var expiringReminder = await db.Reminders
                //.Where(x => x.ExpiresAt < DateTimeOffset.UtcNow)
                .OrderBy(x => x.ExpiresAt)
                .FirstOrDefaultAsync(stoppingToken);

            var delay = expiringReminder?.ExpiresAt - DateTimeOffset.UtcNow;
            var skip = false;
            
            if (delay > MaxDelay)
            {
                Logger.LogDebug("Delay exceeded the maximum value ({Value}). Waiting on a fallback delay and skipping.", MaxDelay.Humanize());
                delay = MaxDelay;
                skip = true;
            }

            if (delay is null) // no reminders awaiting expiry
            {
                try
                {
                    Logger.LogDebug("No reminders in queue - waiting infinitely for CancelCts().");
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
                using var cts = Cts.Linked(_cts.Token);

                try
                {
                    Logger.LogDebug("Waiting for {Delay} for reminder expiry.", delay.Value.Humanize());
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
                    _cts.Dispose();
                    _cts = new();

                    failureCount++;
                    if (failureCount < MAX_FAILURE_COUNT)
                        continue;
                }
            }
            else
            {
                Logger.LogDebug("Timer delay was less than 0 (actual: {Delay}).", delay);
            }
            
            if (failureCount >= MAX_FAILURE_COUNT)
            {
                Logger.LogCritical("Failed {Count} times to run and cancel the delay task. CHECK LOGS!", failureCount);
                Environment.Exit(-1);
            }

            failureCount = 0;

            if (skip)
                continue;

            if (expiringReminder is null)
                continue;

            try
            {
                await expiringReminder.RemindAsync(Bot);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to execute reminder expiry task for reminder {Id}.", expiringReminder.Id);
            }
        }
    }
//#endif
}
#endif