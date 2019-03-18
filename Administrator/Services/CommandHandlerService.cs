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

            var errorEmbed = await BuildErrorEmbedAsync(context, failedResult);
            if (!(errorEmbed is null))
                await context.Channel.SendMessageAsync(embed: errorEmbed);

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

        private async Task<Embed> BuildErrorEmbedAsync(AdminCommandContext context, FailedResult failedResult)
        {
            var builder = new StringBuilder();
            switch (failedResult)
            {
                case ArgumentParseFailedResult argumentParseResult:
                    if (argumentParseResult.Position.HasValue)
                    {
                        // TODO: Replace with context.Path on next Qmmands update
                        var center = context.Prefix.Length + argumentParseResult.Command.FullAliases[0].Length + argumentParseResult.Position.Value;
                        var fullString = $"{context.Prefix}{argumentParseResult.Command.FullAliases[0]} {argumentParseResult.RawArguments}"
                            .FixateTo(ref center, 30 - (context.Prefix.Length + argumentParseResult.Command.FullAliases[0].Length));
                        builder.AppendLine(Format.Code($"{fullString}\n{"↑".PadLeft(center + 2)}"));
                    }
                    builder.AppendLine(argumentParseResult.ArgumentParserFailure switch
                        {
                            ArgumentParserFailure.TooFewArguments => context.Localize("commanderror_toofewarguments",
                                argumentParseResult.Command.Parameters.Count),
                            ArgumentParserFailure.TooManyArguments => context.Localize("commanderror_toomanyarguments",
                                argumentParseResult.Command.Parameters.Count),
                            // TODO: Localize the rest of the errors.
                            _ => argumentParseResult.Reason
                        });
                    break;
                case ChecksFailedResult checkResult:
                    builder.AppendLine(context.Localize("commanderror_checks",
                        string.Join('\n', checkResult.FailedChecks.Select(x => x.Error))));
                    break;
                case CommandNotFoundResult _:
                case CommandOnCooldownResult _:
                    return null;
                case ExecutionFailedResult execResult:
                    await _logging.LogErrorAsync(execResult.Exception, "CommandHandler");
                    var frames = new StackTrace(execResult.Exception, true).GetFrames();
                    var frame = frames.First(x => x.GetFileName()?.Contains("Administrator") == true);
                    var message = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                        $"{execResult.Exception.Message} - at {frame.GetFileName()}, line {frame.GetFileLineNumber()} - {DateTimeOffset.UtcNow:g} UTC"));
                    builder.AppendLine(context.Localize("commanderror_exception",
                        ConfigurationService.SUPPORT_GUILD, Format.Code(message, "cs")));
                    break;
                case OverloadsFailedResult overloadResult:
                    return await BuildErrorEmbedAsync(context,
                        overloadResult.FailedOverloads.Values.FirstOrDefault(x => !(x is ArgumentParseFailedResult)) ??
                        overloadResult.FailedOverloads.Values.First());
                case ParameterChecksFailedResult paramCheckResult:
                    builder.AppendLine(context.Localize("commanderror_paramchecks",
                        string.Join('\n', paramCheckResult.FailedChecks.Select(x => x.Error))));
                    break;
                case TypeParseFailedResult typeParseResult:
                    // TODO: Replace with context.Path
                    builder.AppendLine(Format.Code(
                            $"{context.Prefix}{typeParseResult.Parameter.Command.FullAliases[0]}{typeParseResult.Parameter.Command.FormatArguments()}"))
                        .AppendLine($"\n{Format.Code(typeParseResult.Parameter.Name)}: {typeParseResult.Reason}");
                    break;
            }

            var value = builder.ToString();
            return !string.IsNullOrWhiteSpace(value)
                ? new EmbedBuilder().WithErrorColor().WithTitle(context.Localize("commanderror")).WithDescription(value).Build()
                : null;
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