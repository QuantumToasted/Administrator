using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Administrator.Commands;
using Administrator.Common;
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

        public async Task<bool> TryExecuteCommandAsync(SocketUserMessage userMessage)
        {
            if (userMessage.Source != MessageSource.User || string.IsNullOrWhiteSpace(userMessage.Content)) return false;

            var prefixes = new List<string>
                {_config.DefaultPrefix, $"<@{_client.CurrentUser.Id}> ", $"<@!{_client.CurrentUser.Id}> "};

            LocalizedLanguage language;
            using (var ctx = new AdminDatabaseContext(_provider))
            {
                if (userMessage.Channel is IGuildChannel guildChannel)
                {
                    var guild = await ctx.GetOrCreateGuildAsync(guildChannel.GuildId);
                    prefixes.AddRange(guild.CustomPrefixes);
                    language = guild.Language;
                }
                else
                {
                    var user = await ctx.GetOrCreateGlobalUserAsync(userMessage.Author.Id);
                    language = user.Language;
                }
            }
            
            if (!CommandUtilities.HasAnyPrefix(userMessage.Content, prefixes, StringComparison.OrdinalIgnoreCase,
                out var prefix, out var input)) return false;
            
            var context = new AdminCommandContext(userMessage, prefix, language, _provider);
            var result = await _commands.ExecuteAsync(input, context, _provider);

            if (!(result is FailedResult failedResult)) return true;
            
            // TODO: localized error messages, log stuff
            var builder = new StringBuilder("command_error_placeholder: ");
            switch (failedResult)
            {
                case ParameterChecksFailedResult parameterCheckResult:
                    foreach (var check in parameterCheckResult.FailedChecks)
                    {
                        builder.AppendLine(check.Error);
                    }

                    break;
                case ChecksFailedResult checkResult:
                    foreach (var check in checkResult.FailedChecks)
                    {
                        builder.AppendLine(check.Error);
                    }

                    break;
                case CommandNotFoundResult notFoundResult:
                    return false;
                case ExecutionFailedResult execResult:
                    await _logging.LogErrorAsync(execResult.Exception, "CommandHandler");
                    builder.Append(execResult.Exception.Message);
                    break;
                default:
                    builder.Append(failedResult.Reason);
                    break;
            }

            await context.Channel.SendMessageAsync(builder.ToString());
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
            _commands.AddTypeParser(new ChannelParser<SocketGuildChannel>());
            _commands.AddTypeParser(new ChannelParser<SocketTextChannel>());
            _commands.AddTypeParser(new ChannelParser<SocketVoiceChannel>());
            _commands.AddTypeParser(new ChannelParser<SocketCategoryChannel>());
            _commands.AddTypeParser(new RoleParser<SocketRole>());
            _commands.AddTypeParser(new UserParser<SocketUser>());
            _commands.AddTypeParser(new UserParser<SocketGuildUser>());

            return _logging.LogInfoAsync(modules.SelectMany(x => x.Commands).Count(), "CommandHandler");
        }
    }
}