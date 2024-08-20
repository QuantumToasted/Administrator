using Administrator.Database;
using Disqord.Bot.Hosting;
using Disqord.Utilities.Threading;
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
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);
            
            var now = DateTimeOffset.UtcNow;

            var entries = await db.Members.Where(x => x.NextDemeritPointDecay > now)
                .Select(member => new
                {
                    Member = member,
                    EligibleWarnings = db.Punishments
                        .Include(x => x.Guild)
                        .OfType<Warning>()
                        .Where(x => x.GuildId == member.GuildId && x.Target.Id == (ulong) member.UserId)
                        .Where(x => x.DemeritPointsRemaining > 0 && x.RevokedAt != null)
                        .OrderByDescending(x => x.Id)
                        .ToList()
                })
                .ToListAsync(stoppingToken);

            foreach (var entry in entries)
            {
                if (entry.EligibleWarnings.FirstOrDefault() is not { Guild: var guild } warning)
                {
                    entry.Member.NextDemeritPointDecay = null;
                }
                else
                {
                    warning.DemeritPointsRemaining -= 1;
                    
                    if (entry.EligibleWarnings.Sum(x => x.DemeritPointsRemaining) == 1) // 1 -> 0, set to null
                    {
                        entry.Member.NextDemeritPointDecay = null;
                    }
                    else
                    {
                        entry.Member.NextDemeritPointDecay += guild!.DemeritPointsDecayInterval!.Value;
                    }
                }
            }

            var count = await db.SaveChangesAsync(stoppingToken);
            Logger.LogDebug("Decayed {Count} active warning demerit points.", count);
        }
    }
#endif
}