using Disqord.Bot.Hosting;
using Disqord.Utilities.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Administrator.Bot;

public sealed class ReminderExpiryService : DiscordBotService
{
    private Cts _cts = new();
    
    public void ResetCts()
    {
        if (!_cts.IsCancellationRequested)
            _cts.Cancel();
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Bot.WaitUntilReadyAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);

            var expiredReminder = await db.Reminders
                .Where(x => x.ExpiresAt < DateTimeOffset.UtcNow)
                .OrderByDescending(x => x.ExpiresAt)
                .SingleOrDefaultAsync(stoppingToken);

            var delay = expiredReminder?.ExpiresAt - DateTimeOffset.UtcNow
                        ?? TimeSpan.FromSeconds(30);

            if (delay > TimeSpan.Zero)
            {
                var cts = Cts.Linked(_cts.Token);

                try
                {
                    cts.CancelAfter(delay);
                    await Task.Delay(-1, cts.Token);
                }
                catch (TaskCanceledException) when (_cts.IsCancellationRequested)
                {
                    Logger.LogDebug("Task.Delay canceled due to ResetCts() being called.");
                    
                    _cts.Dispose();
                    _cts = new Cts();
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

            if (expiredReminder is null)
                continue;

            try
            {
                await expiredReminder.RemindAsync(Bot);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to execute reminder expiry task for reminder {Id}.", expiredReminder.Id);
            }
        }
    }
}