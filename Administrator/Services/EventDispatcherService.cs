using System;
using System.Linq;
using System.Threading.Tasks;
using Administrator.Commands;
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
        private readonly TaskQueueService _queue;
        private readonly DiscordShardedClient _client;
        private readonly DiscordRestClient _restClient;
        private readonly CommandService _commands;
        private readonly LoggingService _logging;
        private readonly CommandHandlerService _commandHandler;

        public EventDispatcherService(IServiceProvider provider)
        {
            _provider = provider;
            _queue = _provider.GetRequiredService<TaskQueueService>();
            _client = _provider.GetRequiredService<DiscordShardedClient>();
            _restClient = _provider.GetRequiredService<DiscordRestClient>();
            _commands = _provider.GetRequiredService<CommandService>();
            _logging = _provider.GetRequiredService<LoggingService>();
            _commandHandler = _provider.GetRequiredService<CommandHandlerService>();
        }
       
        Task IService.InitializeAsync()
        {
            _client.Log += message
                => _queue.Enqueue(() => HandleClientLog(message));

            _restClient.Log += message
                => _queue.Enqueue(() => HandleClientLog(message));

            _client.ShardReady += shard
                => _queue.Enqueue(() => HandleShardReady(shard));

            _client.MessageReceived += message
                => _queue.Enqueue(() => HandleMessageReceivedAsync(message));

            _commands.CommandExecuted += (command, result, context, provider)
                => _queue.Enqueue(() => HandleCommandExecutedAsync(command, result, context, provider));

            return _logging.LogDebugAsync("Initialized.", "Configuration");
        }

        private async Task HandleMessageReceivedAsync(SocketMessage message)
        {
            if (!(message is SocketUserMessage userMessage)) return;
            await _commandHandler.TryExecuteCommandAsync(userMessage);
        }

        private async Task HandleCommandExecutedAsync(Command command, CommandResult result, ICommandContext context,
            IServiceProvider provider)
        {
            await _commandHandler.SendCommandResultAsync(command, (AdminCommandResult) result,
                (AdminCommandContext) context, provider);
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