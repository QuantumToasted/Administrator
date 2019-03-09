using System;
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
        private readonly LoggingService _logging;

        public EventDispatcherService(IServiceProvider provider)
        {
            _provider = provider;
            _logging = _provider.GetRequiredService<LoggingService>();
        }
       
        public async Task InitializeAsync()
        {
            var client = _provider.GetRequiredService<DiscordShardedClient>();
            var restClient = _provider.GetRequiredService<DiscordRestClient>();

            client.Log += OnLog;
            restClient.Log += OnLog;

            await _logging.LogDebugAsync("Initialized.", "Configuration");
        }

        private Task OnLog(LogMessage message)
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
    }
}