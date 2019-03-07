using System;
using System.Linq;
using System.Threading.Tasks;
using Administrator.Common;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;

namespace Administrator.Services
{
    public sealed class EventDispatcherService : IService
    {
        private readonly IServiceProvider _provider;

        public EventDispatcherService(IServiceProvider provider)
        {
            _provider = provider;
        }
       
        public Task InitializeAsync()
        {
            var client = _provider.GetRequiredService<DiscordShardedClient>();
            var restClient = _provider.GetRequiredService<DiscordRestClient>();

            client.Log += OnLog;
            restClient.Log += OnLog;

            Log.Verbose("Initialized.");
            return Task.CompletedTask;
        }

        private Task OnLog(LogMessage message)
        {
            if (string.IsNullOrWhiteSpace(message.Message)) return Task.CompletedTask;

            switch (message.Severity)
            {
                case LogSeverity.Critical:
                    Log.Critical(message.Message, message.Source);
                    break;
                case LogSeverity.Error:
                    Log.Error(message.Message, message.Source);
                    break;
                case LogSeverity.Warning:
                    Log.Warning(message.Message, message.Source);
                    break;
                case LogSeverity.Info:
                    Log.Info(message.Message, message.Source);
                    break;
                case LogSeverity.Verbose:
                    Log.Verbose(message.Message, message.Source);
                    break;
                case LogSeverity.Debug:
                    Log.Debug(message.Message, message.Source);
                    break;
            }

            return Task.CompletedTask;
        }
    }
}