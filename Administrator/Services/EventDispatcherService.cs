using System;
using System.Threading.Tasks;
using Administrator.Commands;
using Administrator.Common;
using Administrator.Database;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using FluentScheduler;
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
        private readonly PaginationService _pagination;
        private readonly PunishmentService _punishments;
        private readonly LevelService _levels;
        private bool _firstReady;

        public EventDispatcherService(IServiceProvider provider)
        {
            _provider = provider;
            _queue = _provider.GetRequiredService<TaskQueueService>();
            _client = _provider.GetRequiredService<DiscordSocketClient>();
            _restClient = _provider.GetRequiredService<DiscordRestClient>();
            _commands = _provider.GetRequiredService<CommandService>();
            _logging = _provider.GetRequiredService<LoggingService>();
            _commandHandler = _provider.GetRequiredService<CommandHandlerService>();
            _pagination = _provider.GetRequiredService<PaginationService>();
            _punishments = _provider.GetRequiredService<PunishmentService>();
            _levels = _provider.GetRequiredService<LevelService>();
        }

        private async Task HandleUserBannedAsync(SocketUser user, SocketGuild guild)
        {
            if (_punishments.BannedUserIds.Remove(user.Id)) return;

            using (var ctx = new AdminDatabaseContext(_provider))
            {
                var config = await ctx.GetOrCreateGuildAsync(guild.Id);
                if (config.Settings.HasFlag(GuildSettings.Punishments | GuildSettings.AutoPunishments))
                {
                    await _punishments.LogBanAsync(user, guild, null);
                }
            }
        }

        private async Task HandleUserLeftAsync(SocketGuildUser user)
        {
            if (_punishments.KickedUserIds.Remove(user.Id)) return;

            using (var ctx = new AdminDatabaseContext(_provider))
            {
                var config = await ctx.GetOrCreateGuildAsync(user.Guild.Id);
                if (config.Settings.HasFlag(GuildSettings.Punishments | GuildSettings.AutoPunishments))
                {
                    await _punishments.LogKickAsync(user, user.Guild, null);
                }
            }
        }

        private async Task HandleReactionAddedAsync(Cacheable<IUserMessage, ulong> cacheable,
            ISocketMessageChannel channel, SocketReaction reaction)
        {
            await _pagination.ModifyPaginatorsAsync(cacheable, channel, reaction);
        }

        private async Task HandleMessageReceivedAsync(SocketMessage message)
        {
            if (!(message is SocketUserMessage userMessage)) return;
            var executed = await _commandHandler.TryExecuteCommandAsync(userMessage);

            if (!executed)
            {
                await _levels.IncrementXpAsync(userMessage);
            }
        }

        private async Task HandleCommandExecutedAsync(CommandResult result, CommandContext context)
        {
            await _commandHandler.SendCommandResultAsync(result.Command, (AdminCommandResult) result,
                (AdminCommandContext) context);
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

        private Task HandleReady()
        {
            if (_firstReady)
                return Task.CompletedTask;

            JobManager.Initialize(_provider.GetRequiredService<Registry>());
            _firstReady = true;
            return Task.CompletedTask;
        }

        Task IService.InitializeAsync()
        {
            _client.Ready += ()
                => _queue.Enqueue(HandleReady);

            _client.Log += message
                => _queue.Enqueue(() => HandleClientLog(message));

            _client.MessageReceived += message
                => _queue.Enqueue(() => HandleMessageReceivedAsync(message));

            _client.ReactionAdded += (cacheable, channel, reaction)
                => _queue.Enqueue(() => HandleReactionAddedAsync(cacheable, channel, reaction));

            _client.UserBanned += (user, guild)
                => _queue.Enqueue(() => HandleUserBannedAsync(user, guild));

            _client.UserLeft += user
                => _queue.Enqueue(() => HandleUserLeftAsync(user));

            _restClient.Log += message
                => _queue.Enqueue(() => HandleClientLog(message));

            _commands.CommandExecuted += args
                => _queue.Enqueue(() => HandleCommandExecutedAsync(args.Result, args.Context));

            return _logging.LogInfoAsync("Initialized.", "Dispatcher");
        }
    }
}