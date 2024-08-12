using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Microsoft.Extensions.Logging;

namespace Administrator.Bot;

public sealed class GuildChunkService : DiscordBotService
{
    private readonly HashSet<Snowflake> _chunkedGuildIds = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Bot.WaitUntilReadyAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var oldCount = _chunkedGuildIds.Count;
            
            foreach (var guild in Bot.GetGuilds().Values.OrderByDescending(x => x.MemberCount))
            {
                if (_chunkedGuildIds.Contains(guild.Id))
                {
                    Logger.LogTrace("Guild {GuildId} is already chunked - skipping.", guild.Id.RawValue);
                    continue;
                }
                
                if (!await Bot.Chunker.ChunkAsync(guild, stoppingToken))
                {
                    Logger.LogTrace("Guild {GuildId} does not require chunking.", guild.Id.RawValue);
                    continue;
                }

                _chunkedGuildIds.Add(guild.Id);
                Logger.LogDebug("Chunking completed for guild {GuildId}.", guild.Id.RawValue);

                var chunkDelaySeconds = Random.Shared.Next(1, 4);
                Logger.LogTrace("Delaying next chunk by {Seconds} seconds.", chunkDelaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(chunkDelaySeconds), stoppingToken);
            }

            if (_chunkedGuildIds.Count > oldCount)
            {
                Logger.LogInformation("Successfully chunked {Count} guild(s).", _chunkedGuildIds.Count - oldCount);
            }
            
            var loopDelaySeconds = Random.Shared.Next(10, 20);
            Logger.LogTrace("Delaying next chunk loop by {Seconds} seconds.", loopDelaySeconds);
            await Task.Delay(TimeSpan.FromSeconds(loopDelaySeconds), stoppingToken);
        }
    }
}