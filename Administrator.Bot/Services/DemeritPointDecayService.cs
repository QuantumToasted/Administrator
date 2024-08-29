using System.Collections.Concurrent;
using Administrator.Database;
using Disqord;
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

            try
            {
                await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);

                var now = DateTimeOffset.UtcNow;
                var rawEntries = await db.Members.Where(x => x.NextDemeritPointDecay < now)
                    .Select(member => new
                    {
                        Member = member,
                        Punishments = db.Punishments
                            .Where(x => x.GuildId == member.GuildId && x.Target.Id == (ulong)member.UserId)
                            .OrderBy(x => x.Id) // decay oldest to newest
                            .ToList()
                    })
                    //.Where(x => x.Punishments.Count > 0)
                    .ToListAsync(stoppingToken);

                /*
                var punishments = await db.Punishments.ToListAsync(stoppingToken);
                var members = await db.Members.Where(x => x.NextDemeritPointDecay < now)
                    //.Where(x => db.Punishments.Count(y => y.GuildId == x.GuildId && y.Target.Id == (ulong) x.UserId) > 0)
                    .ToListAsync(stoppingToken);

                var rawEntries = members.Select(member => new
                    {
                        Member = member,
                        Punishments = punishments
                            .Where(x => x.GuildId == member.GuildId && x.Target.Id == member.UserId)
                            .OrderBy(x => x.Id) // decay oldest to newest
                            .ToList()
                    })
                    .Where(x => x.Punishments.Count > 0)
                    .ToList();
                */

                 var entries = rawEntries.Select(entry => new
                    {
                        entry.Member,
                        EligibleWarnings = entry.Punishments.OfType<Warning>()
                            .Where(x => x.DemeritPointsRemaining > 0)
                            .OrderBy(x => x.Id),
                        ActiveBan = entry.Punishments.OfType<Ban>()
                            .OrderByDescending(x => x.Id)
                            .FirstOrDefault(x => x.RevokedAt == null)
                    })
                    .Where(x => x.ActiveBan == null)
                    .ToList();

                var guildCache = new Dictionary<Snowflake, Guild>();
                foreach (var entry in entries)
                {
                    if (!guildCache.TryGetValue(entry.Member.GuildId, out var guild))
                    {
                        guild = guildCache[entry.Member.GuildId] = await db.Guilds.GetOrCreateAsync(entry.Member.GuildId);
                    }

                    if (entry.EligibleWarnings.FirstOrDefault() is not { } warning)
                    {
                        Logger.LogDebug("Setting user {UserId} in guild {GuildId}'s DP decay to null because they don't have any eligible warnings.",
                            entry.Member.UserId.RawValue, entry.Member.GuildId.RawValue);
                        entry.Member.NextDemeritPointDecay = null;
                    }
                    else
                    {
                        warning.DemeritPointsRemaining -= 1;

                        if (entry.EligibleWarnings.Sum(x => x.DemeritPointsRemaining) == 0) // 1 -> 0, set to null
                        {
                            Logger.LogDebug("Setting user {UserId} in guild {GuildId}'s DP decay to null because they are decaying from 1 -> 0 DPs.",
                                entry.Member.UserId.RawValue, entry.Member.GuildId.RawValue);
                            entry.Member.NextDemeritPointDecay = null;
                        }
                        else
                        {
                            var newValue = entry.Member.NextDemeritPointDecay + guild.DemeritPointsDecayInterval!.Value;

                            if (warning.DemeritPointsRemaining == 0 && entry.EligibleWarnings
                                    .Where(x => x.DemeritPointsRemaining > 0 && // If the next warning has DPs remaining
                                                x.DemeritPointsRemaining == x.DemeritPoints && // And hasn't decayed
                                                x.Id != warning.Id) // And is not the warning we're decaying
                                    .MinBy(x => x.Id) is { } nextWarning && nextWarning.CreatedAt > newValue)
                            {
                                newValue = nextWarning.CreatedAt + guild.DemeritPointsDecayInterval!.Value;
                                Logger.LogDebug("Setting user {UserId} in guild {GuildId}'s DP decay to {Value} because they have a warning newer than the next decay.", entry.Member.UserId.RawValue, entry.Member.GuildId.RawValue, newValue);
                            }
                            
                            //Logger.LogDebug("Setting user {UserId} in guild {GuildId}'s DP decay to {Value}.", entry.Member.UserId.RawValue, entry.Member.GuildId.RawValue, newValue);
                            //entry.Member.NextDemeritPointDecay += guild.DemeritPointsDecayInterval!.Value;
                            entry.Member.NextDemeritPointDecay = newValue;
                        }
                    }
                }

                var count = await db.SaveChangesAsync(stoppingToken);
                if (count > 0)
                {
                    // count / 2 because 2 rows are updated
                    Logger.LogDebug("Decayed {Count} active warning demerit points.", count / 2);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to decay active warning demerit points.");
            }
        }
    }
#endif
}