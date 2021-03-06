using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Administrator.Extensions;
using Disqord;
using Disqord.Events;
using Disqord.Logging;
using FluentScheduler;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;

namespace Administrator.Services
{
    public sealed class EventDispatcherService : Service,
        IHandler<ReadyEventArgs>
    {
        private readonly IDictionary<Type, List<IHandler>> _handlers;
        private readonly TaskQueueService _queue;
        private readonly DiscordClient _client;
        private readonly CommandService _commands;
        private readonly LoggingService _logging;
        private bool _firstReady;

        public EventDispatcherService(IServiceProvider provider)
            : base(provider)
        {
            _handlers = new Dictionary<Type, List<IHandler>>();
            _queue = _provider.GetRequiredService<TaskQueueService>();
            _client = _provider.GetRequiredService<DiscordClient>();
            _commands = _provider.GetRequiredService<CommandService>();
            _logging = _provider.GetRequiredService<LoggingService>();
        }

        public Task HandleAsync(ReadyEventArgs args)
        {
            if (_firstReady)
                return Task.CompletedTask;

            JobManager.Initialize(_provider.GetRequiredService<Registry>());
            _firstReady = true;

            return Task.CompletedTask;
        }

        public override async Task InitializeAsync()
        {
            var types = typeof(DiscordEventArgs).Assembly.GetTypes()
                .Concat(typeof(MessageLoggedEventArgs).Assembly.GetTypes())
                .Concat(typeof(CommandService).Assembly.GetTypes());

            foreach (var type in types.Where(x => typeof(EventArgs).IsAssignableFrom(x) && !x.IsAbstract))
            {
                await _logging.LogDebugAsync($"Created handler group for {type}", "Events");
                _handlers[type] = _provider.GetHandlers(type).ToList();
            }

            _client.Ready += EnqueueHandlers;
            _client.Logger.MessageLogged += (_, args) => EnqueueHandlers(args);
            _client.MessageReceived += EnqueueHandlers;
            _client.MessageDeleted += EnqueueHandlers;
            _client.MessageUpdated += EnqueueHandlers;
            _client.ReactionAdded += EnqueueHandlers;
            _client.ReactionRemoved += EnqueueHandlers;
            _client.MemberBanned += EnqueueHandlers;
            _client.MemberLeft += EnqueueHandlers;
            _client.MemberJoined += EnqueueHandlers;
            _client.MemberUpdated += EnqueueHandlers;
            _client.UserUpdated += EnqueueHandlers;
            _client.ReactionsCleared += EnqueueHandlers;
            _client.ChannelDeleted += EnqueueHandlers;
            _client.MessagesBulkDeleted += EnqueueHandlers;

            _commands.CommandExecuted += EnqueueHandlers;
            _commands.CommandExecutionFailed += EnqueueHandlers;

            await _logging.LogInfoAsync("Initialized.", "Dispatcher");
        }

        private Task EnqueueHandlers<TArgs>(TArgs args)
            where TArgs : EventArgs
        {
            foreach (var handler in _handlers[typeof(TArgs)])
            {
                _queue.Enqueue(() => ((IHandler<TArgs>) handler).HandleAsync(args));
            }

            return Task.CompletedTask;
        }
    }
}