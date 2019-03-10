using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace Administrator.Services
{
    public sealed class EventDispatcherService : IService
    {
        private readonly IServiceProvider _provider;
        private readonly DiscordShardedClient _client;
        private readonly LoggingService _logging;
        private readonly TaskQueueService _queue;

        public EventDispatcherService(IServiceProvider provider)
        {
            _provider = provider;
            _client = _provider.GetRequiredService<DiscordShardedClient>();
            _logging = _provider.GetRequiredService<LoggingService>();
            _queue = _provider.GetRequiredService<TaskQueueService>();
        }
       
        async Task IService.InitializeAsync()
        {
            var restClient = _provider.GetRequiredService<DiscordRestClient>();

            _client.Log += message
                => _queue.Enqueue(() => HandleClientLog(message));

            restClient.Log += message
                => _queue.Enqueue(() => HandleClientLog(message));

            _client.ShardReady += shard
                => _queue.Enqueue(() => HandleShardReady(shard));

            await _logging.LogDebugAsync("Initialized.", "Configuration");
        }

        private Task HandleClientLog(LogMessage message)
        {
            if (string.IsNullOrWhiteSpace(message.Message)) return Task.CompletedTask;

            switch (message.Severity)
            {
                case LogSeverity.Critical:
                    return _logging.LogCriticalAsync(message.Message, message.Source);
                case LogSeverity.Error:
                    return _logging.LogErrorAsync(message.Message, message.Source);
                case LogSeverity.Warning:
                    return _logging.LogWarningAsync(message.Message, message.Source);
                case LogSeverity.Info:
                    return _logging.LogInfoAsync(message.Message, message.Source);
                case LogSeverity.Verbose:
                    return _logging.LogVerboseAsync(message.Message, message.Source);
                case LogSeverity.Debug:
                    return _logging.LogDebugAsync(message.Message, message.Source);
                default:
                    return Task.CompletedTask;
            }
        }

        private Task HandleShardReady(BaseSocketClient shard)
        {
            // TODO: Track total shard(s) ready
            return _logging.LogInfoAsync("Ready", $"Shard {_client.GetShardIdFor(shard.Guilds.First())}");
        }
    }
}