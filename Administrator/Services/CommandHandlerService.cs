using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Administrator.Commands;
using Administrator.Database;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;

namespace Administrator.Services
{
    public sealed class CommandHandlerService : IService
    {
        private readonly IServiceProvider _provider;
        private readonly CommandService _commands;
        private readonly DiscordShardedClient _client;
        private readonly ConfigurationService _config;
        private readonly LoggingService _logging;

        public CommandHandlerService(IServiceProvider provider)
        {
            _provider = provider;
            _commands = _provider.GetRequiredService<CommandService>();
            _client = _provider.GetRequiredService<DiscordShardedClient>();
            _config = _provider.GetRequiredService<ConfigurationService>();
            _logging = _provider.GetRequiredService<LoggingService>();
        }

        public async Task<bool> TryExecuteCommandAsync(SocketUserMessage message)
        {
            if (message.Source != MessageSource.User || string.IsNullOrWhiteSpace(message.Content)) return false;

            var prefixes = new List<string>
                {_config.DefaultPrefix, $"<@{_client.CurrentUser.Id}> ", $"<@!{_client.CurrentUser.Id}> "};

            using (var ctx = new AdminDatabaseContext(_provider))
            {
                if (message.Channel is IGuildChannel guildChannel)
                {
                    var guild = await ctx.GetOrCreateGuildAsync(guildChannel.GuildId);
                    prefixes.AddRange(guild.CustomPrefixes);
                }
            }
            
            if (!CommandUtilities.HasAnyPrefix(message.Content, prefixes, StringComparison.OrdinalIgnoreCase,
                out var prefix, out var input)) return false;
            
            var context = new AdminCommandContext(_client, message, prefix, _provider);
            var result = await _commands.ExecuteAsync(input, context, _provider);

            if (!(result is FailedResult failedResult)) return true;
            
            // TODO: localized error messages, log stuff
            await context.Channel.SendMessageAsync(failedResult.Reason);

            if (failedResult is ExecutionFailedResult r)
                await _logging.LogErrorAsync(r.Exception, "CommandHandler");
            return false;
        }

        public async Task SendCommandResultAsync(Command command, AdminCommandResult result,
            AdminCommandContext context, IServiceProvider provider)
        {
            // TODO: Log stuff
            if (!(result.File is null))
            {
                await context.Channel.SendFileAsync(result.File.Stream, result.File.Filename,
                    result.Text ?? string.Empty, embed: result.Embed);
                return;
            }

            if (!string.IsNullOrWhiteSpace(result.Text) || !(result.Embed is null))
            {
                await context.Channel.SendMessageAsync(result.Text ?? string.Empty, embed: result.Embed);
            }
        }
        
        Task IService.InitializeAsync()
        {
            var modules = _commands.AddModules(Assembly.GetEntryAssembly());
            // TODO: Add TypeParsers
            return _logging.LogInfoAsync(modules.SelectMany(x => x.Commands).Count(), "CommandHandler");
        }
    }
}