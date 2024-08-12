using Administrator.Database;
using Disqord.Bot.Hosting;
using Disqord.Utilities.Threading;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Administrator.Bot;

public sealed class PunishmentExpiryService : DiscordBotService
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

            var punishments = await db.Punishments.OfType<RevocablePunishment>().ToListAsync(stoppingToken);

            var expiringPunishment = punishments.Select(x => new { Punishment = x, (x as IExpiringDbEntity)?.ExpiresAt })
                .Where(x => x.ExpiresAt.HasValue && !x.Punishment.RevokedAt.HasValue).MinBy(x => x.ExpiresAt!.Value);

            var delay = expiringPunishment?.ExpiresAt - DateTimeOffset.UtcNow;

            if (delay is null)
            {
                try
                {
                    Logger.LogDebug("No punishments in queue - waiting infinitely for CancelCts().");
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
                    Logger.LogDebug("Waiting for {Delay} for punishment expiry.", delay);
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

            if (expiringPunishment is null)
                continue;

            var punishmentService = scope.ServiceProvider.GetRequiredService<PunishmentService>();
            var name = expiringPunishment.Punishment.FormatPunishmentName(LetterCasing.Sentence);

            try
            {
                await punishmentService.RevokePunishmentAsync(expiringPunishment.Punishment.GuildId, expiringPunishment.Punishment.Id, Bot.CurrentUser, $"{name} expired.", false);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to revoke expiring {Name} {Id}.", expiringPunishment.GetType().Name, expiringPunishment.Punishment.Id);
            }
        }
    }
#endif

}