using System;
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
        private readonly DiscordSocketClient _client;
        private readonly DiscordRestClient _restClient;
        private readonly CommandService _commands;
        private readonly LoggingService _logging;
        private readonly CommandHandlerService _commandHandler;

        public EventDispatcherService(IServiceProvider provider)
        {
            _provider = provider;
            _queue = _provider.GetRequiredService<TaskQueueService>();
            _client = _provider.GetRequiredService<DiscordSocketClient>();
            _restClient = _provider.GetRequiredService<DiscordRestClient>();
            _commands = _provider.GetRequiredService<CommandService>();
            _logging = _provider.GetRequiredService<LoggingService>();
            _commandHandler = _provider.GetRequiredService<CommandHandlerService>();
        }
       
        Task IService.InitializeAsync()
        {
            _client.Log += message
                => _queue.Enqueue(() => HandleClientLog(message));

            _client.MessageReceived += message
                => _queue.Enqueue(() => HandleMessageReceivedAsync(message));

            _restClient.Log += message
                => _queue.Enqueue(() => HandleClientLog(message));

            _commands.CommandExecuted += args
                => _queue.Enqueue(() => HandleCommandExecutedAsync(args.Result, args.Context, args.Provider));

            return _logging.LogInfoAsync("Initialized.", "Dispatcher");
        }

        private async Task HandleMessageReceivedAsync(SocketMessage message)
        {
            if (!(message is SocketUserMessage userMessage)) return;
            await _commandHandler.TryExecuteCommandAsync(userMessage);
        }

        private async Task HandleCommandExecutedAsync(CommandResult result, CommandContext context,
            IServiceProvider provider)
        {
            await _commandHandler.SendCommandResultAsync(result.Command, (AdminCommandResult) result,
                (AdminCommandContext) context, provider);
        }

        private Task HandleClientLog(LogMessage message)
        {
            if (string.IsNullOrWhiteSpace(message.Message)) return Task.CompletedTask;

            return message.Severity switch
            {
                LogSeverity.Critical => _logging.LogCriticalAsync(message.Message, "Discord"),
                LogSeverity.Error => _logging.LogErrorAsync(message.Message, "Discord"),
                LogSeverity.Warning => _logging.LogWarningAsync(message.Message, "Discord"),
                LogSeverity.Info => _logging.LogInfoAsync(message.Message, "Discord"),
                LogSeverity.Verbose => _logging.LogVerboseAsync(message.Message, "Discord"),
                LogSeverity.Debug => _logging.LogVerboseAsync(message.Message, "Discord"),
                _ => Task.CompletedTask
            };
        }
    }
}