using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Administrator.Commands;
using Administrator.Common;
using Administrator.Database;
using Administrator.Extensions;
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
        private readonly DiscordSocketClient _client;
        private readonly ConfigurationService _config;
        private readonly LoggingService _logging;

        public CommandHandlerService(IServiceProvider provider)
        {
            _provider = provider;
            _commands = _provider.GetRequiredService<CommandService>();
            _client = _provider.GetRequiredService<DiscordSocketClient>();
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
            
            // TODO: localized error messages, log 
            var error = new StringBuilder()
                .Append(failedResult switch
            {
                ParameterChecksFailedResult parameterChecksResult => string.Join('\n', parameterChecksResult.FailedChecks.Select(x => x.Error)),
                ChecksFailedResult checkResult => string.Join('\n', checkResult.FailedChecks.Select(x => x.Error)),
                ExecutionFailedResult execResult => GenerateException(execResult.Exception),
                CommandNotFoundResult _ => string.Empty,
                _ => failedResult.Reason
            }).ToString();

            if (!string.IsNullOrWhiteSpace(error))
                await context.Channel.SendMessageAsync(embed: new EmbedBuilder().WithErrorColor()
                    .WithTitle(context.Localize("commanderror")).WithDescription(error).Build());
            
            return false;

            string GenerateException(Exception ex)
            {
                _logging.LogErrorAsync(ex, "CommandHandler");
                var frames = new StackTrace(ex, true).GetFrames();
                var frame = frames.First(x => x.GetFileName()?.Contains("Administrator") == true);
                var message = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                    $"{ex.Message} - at {frame.GetFileName()}, line {frame.GetFileLineNumber()} - {DateTimeOffset.UtcNow:g} UTC"));
                return context.Localize("commanderror_exception", ConfigurationService.SUPPORT_GUILD, message);
            }
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