using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Administrator.Commands;
using Administrator.Common;
using Administrator.Database;
using Administrator.Extensions;
using Disqord;
using Disqord.Events;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Qmmands;

namespace Administrator.Services
{
    public sealed class CommandHandlerService : IService, IHandler<MessageReceivedEventArgs>, 
        IHandler<CommandExecutedEventArgs>, IHandler<CommandExecutionFailedEventArgs>
    {
        private readonly IServiceProvider _provider;
        private readonly CommandService _commands;
        private readonly DiscordClient _client;
        private readonly ConfigurationService _config;
        private readonly LoggingService _logging;

        public CommandHandlerService(IServiceProvider provider)
        {
            _provider = provider;
            _commands = _provider.GetRequiredService<CommandService>();
            _client = _provider.GetRequiredService<DiscordClient>();
            _config = _provider.GetRequiredService<ConfigurationService>();
            _logging = _provider.GetRequiredService<LoggingService>();
        }

        public async Task HandleAsync(MessageReceivedEventArgs args)
        {
            if (!(args.Message is CachedUserMessage message) ||
                message.Author.IsBot || string.IsNullOrWhiteSpace(message.Content))
                return;

            var prefixes = new List<string>
                {_config.DefaultPrefix, $"<@{_client.CurrentUser.Id}> ", $"<@!{_client.CurrentUser.Id}> "};

            LocalizedLanguage language;
            using var ctx = new AdminDatabaseContext(_provider);

            if (message.Channel is IGuildChannel guildChannel)
            {
                var guild = await ctx.GetOrCreateGuildAsync(guildChannel.GuildId);
                prefixes.AddRange(guild.CustomPrefixes);
                language = guild.Language;
            }
            else
            {
                var user = await ctx.GetOrCreateGlobalUserAsync(message.Author.Id);
                language = user.Language;
            }

            if (!CommandUtilities.HasAnyPrefix(message.Content, prefixes, StringComparison.OrdinalIgnoreCase,
                out var prefix, out var input)) return;

            var context = new AdminCommandContext(message, prefix, language, _provider);
            var result = await _commands.ExecuteAsync(input, context);

            if (!(result is FailedResult failedResult)) return;

            var errorEmbed = await BuildErrorEmbedAsync(context, failedResult);
            if (!(errorEmbed is null))
                await context.Channel.SendMessageAsync(embed: errorEmbed);

            return;
        }

        public async Task HandleAsync(CommandExecutedEventArgs args)
        {
            var context = (AdminCommandContext) args.Context;
            var result = (AdminCommandResult) args.Result;

            // TODO: Log stuff
            if (result.Attachment is { })
            {
                await context.Channel.SendMessageAsync(result.Attachment, result.Text ?? string.Empty,
                    embed: result.Embed);
                // result.File.Dispose();
                return;
            }

            if (!string.IsNullOrWhiteSpace(result.Text) || !(result.Embed is null))
            {
                await context.Channel.SendMessageAsync(result.Text ?? string.Empty, embed: result.Embed);
            }
        }

        private async Task<LocalEmbed> BuildErrorEmbedAsync(AdminCommandContext context, FailedResult failedResult)
        {
            var builder = new StringBuilder();
            switch (failedResult)
            {
                case ArgumentParseFailedResult argumentResult when argumentResult.ParserResult is DefaultArgumentParserResult parserResult:
                    if (parserResult.FailurePosition.HasValue)
                    {
                        var path = string.Join(' ', context.Path);
                        var center = context.Prefix.Length + path.Length + parserResult.FailurePosition.Value;
                        var fullString = $"{context.Prefix}{path} {argumentResult.RawArguments}"
                            .FixateTo(ref center, 30 - (context.Prefix.Length + path.Length));
                        builder.AppendLine(Markdown.CodeBlock($"{fullString}\n{"↑".PadLeft(center + 2)}"));
                    }
                    builder.AppendLine(parserResult.Failure.GetValueOrDefault() switch
                    {
                        DefaultArgumentParserFailure.TooFewArguments => context.Localize("commanderror_toofewarguments",
                            argumentResult.Command.Parameters.Count(x => !x.IsOptional)),
                        DefaultArgumentParserFailure.TooManyArguments => context.Localize("commanderror_toomanyarguments",
                            argumentResult.Command.Parameters.Count),
                        // TODO: Localize the rest of the errors.
                        _ => argumentResult.Reason
                    });
                    break;
                case ChecksFailedResult checkResult when checkResult.FailedChecks.OfType<RequireOwnerAttribute>().Any():
                    return null;
                case ChecksFailedResult checkResult:
                    builder.AppendLine(context.Localize("commanderror_checks",
                        string.Join('\n', checkResult.FailedChecks.Select(x => x.Result.Reason))));
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
                        ConfigurationService.SUPPORT_GUILD, Markdown.CodeBlock(message)));
                    break;
                case OverloadsFailedResult overloadResult:
                    return await BuildErrorEmbedAsync(context,
                        overloadResult.FailedOverloads.Values.FirstOrDefault(x => !(x is ArgumentParseFailedResult)) ??
                        overloadResult.FailedOverloads.Values.First());
                case ParameterChecksFailedResult paramCheckResult:
                    builder.AppendLine(context.Localize("commanderror_paramchecks",
                        string.Join('\n', paramCheckResult.FailedChecks.Select(x => x.Result.Reason))));
                    break;
                case TypeParseFailedResult typeParseResult:
                    builder.AppendLine(Markdown.Code(
                            $"{context.Prefix}{string.Join(' ', context.Path)}{typeParseResult.Parameter.Command.FormatArguments()}"))
                        .AppendLine($"\n{Markdown.Code(typeParseResult.Parameter.Name)}: {typeParseResult.Reason}");
                    break;
            }

            var value = builder.ToString();
            return !string.IsNullOrWhiteSpace(value)
                ? new LocalEmbedBuilder().WithErrorColor().WithTitle(context.Localize("commanderror")).WithDescription(value).Build()
                : null;
        }

        public Task HandleAsync(CommandExecutionFailedEventArgs args)
            => _logging.LogErrorAsync(args.Result.Exception, "CommandHandler");

        async Task IService.InitializeAsync()
        {
            var modules = _commands.AddModules(Assembly.GetEntryAssembly(), action: builder =>
            {
                foreach (var command in CommandUtilities.EnumerateAllCommands(builder))
                {
                    command.AddCheck(new RequirePermissionsAttribute());
                }
            });

            // TODO: Add TypeParsers
            _commands.AddTypeParser(new ChannelParser<CachedGuildChannel>());
            _commands.AddTypeParser(new ChannelParser<CachedTextChannel>());
            _commands.AddTypeParser(new ChannelParser<CachedVoiceChannel>());
            _commands.AddTypeParser(new ChannelParser<CachedCategoryChannel>());
            _commands.AddTypeParser(new RoleParser());
            _commands.AddTypeParser(new UserParser<CachedUser>());
            _commands.AddTypeParser(new UserParser<CachedMember>());
            _commands.AddTypeParser(new GuildParser());
            _commands.AddTypeParser(new TimeSpanParser());
            _commands.AddTypeParser(new ColorParser());
            _commands.AddTypeParser(new RegexParser());
            _commands.AddTypeParser(new MassPunishmentParser<MassWarning>());
            _commands.AddTypeParser(new MassPunishmentParser<MassMute>());
            _commands.AddTypeParser(new MassPunishmentParser<MassBan>());
            _commands.AddTypeParser(new ModuleParser());
            _commands.AddTypeParser(new LanguageParser());
            _commands.AddTypeParser(new EmojiParser());

            // TODO: A better place to put this? A better way to do this?
            var set = new HashSet<string>();
            foreach (var command in _commands.GetAllCommands())
            {
                set.Add($"info_command_{command.FullAliases[0].Replace(' ', '_')}");
            }

            var language =
                JsonConvert.DeserializeObject<LocalizedLanguage>(
                    await File.ReadAllTextAsync("./Data/Responses/en-US.json"));
            var responses = language.Responses.ToDictionary(x => x.Key, x => x.Value);
            foreach (var key in set)
            {
                if (responses.TryAdd(key, new[] {""}.ToImmutableArray()))
                {
                    await _logging.LogDebugAsync($"Added key {key}.", "CommandHandler");
                }
            }
            language.UpdateResponses(responses);
            await File.WriteAllTextAsync("./Data/Responses/en-US.json", JsonConvert.SerializeObject(language, Formatting.Indented));

            await _logging.LogInfoAsync(modules.SelectMany(x => x.Commands).Count(), "CommandHandler");
        }
    }
}