using Disqord.Bot.Hosting;
using Disqord.Utilities.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Administrator.Bot;

public sealed class ReminderExpiryService : DiscordBotService
{
    private Cts _cts = new();
    
    public void CancelCts()
    {
        if (!_cts.IsCancellationRequested)
            _cts.Cancel();

        //_cts = new();
    }
    
#if !MIGRATING
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Bot.WaitUntilReadyAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);

            var expiringReminder = await db.Reminders
                //.Where(x => x.ExpiresAt < DateTimeOffset.UtcNow)
                .OrderBy(x => x.ExpiresAt)
                .FirstOrDefaultAsync(stoppingToken);

            var delay = expiringReminder?.ExpiresAt - DateTimeOffset.UtcNow;

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
                var cts = Cts.Linked(_cts.Token);

                try
                {
                    Logger.LogDebug("Waiting for {Delay} for reminder expiry.", delay);
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
#endif
}