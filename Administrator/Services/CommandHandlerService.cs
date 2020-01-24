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
using Administrator.Common.LocalizedEmbed;
using Administrator.Database;
using Administrator.Extensions;
using Disqord;
using Disqord.Events;
using Humanizer;
using Humanizer.Localisation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Qmmands;

namespace Administrator.Services
{
    public sealed class CommandHandlerService : Service, 
        IHandler<MessageReceivedEventArgs>, 
        IHandler<CommandExecutedEventArgs>, 
        IHandler<CommandExecutionFailedEventArgs>
    {
        private readonly IDictionary<Snowflake, IEnumerable<string>> _guildPrefixes;
        private readonly IDictionary<Snowflake, LocalizedLanguage> _languages;
        private readonly CommandService _commands;
        private readonly ConfigurationService _config;
        private readonly LoggingService _logging;
        private readonly LocalizationService _localization;

        public CommandHandlerService(IServiceProvider provider)
            : base(provider)
        {
            _guildPrefixes = new Dictionary<Snowflake, IEnumerable<string>>();
            _languages = new Dictionary<Snowflake, LocalizedLanguage>();

            _commands = _provider.GetRequiredService<CommandService>();
            _config = _provider.GetRequiredService<ConfigurationService>();
            _logging = _provider.GetRequiredService<LoggingService>();
            _localization = _provider.GetRequiredService<LocalizationService>();
        }

        public void UpdateLanguage(Snowflake id, LocalizedLanguage newLanguage)
        {
            _languages[id] = newLanguage;
        }

        public void UpdatePrefixes(Snowflake guildId, IEnumerable<string> newPrefixes)
        {
            _guildPrefixes[guildId] = newPrefixes;
        }

        public async Task HandleAsync(MessageReceivedEventArgs args)
        {
            if (!(args.Message is CachedUserMessage message) ||
                message.Author.IsBot || string.IsNullOrWhiteSpace(message.Content))
                return;

            var prefixes = new List<string> {_config.DefaultPrefix};

            LocalizedLanguage language;
            using var ctx = new AdminDatabaseContext(_provider);

            if (message.Channel is IGuildChannel guildChannel)
            {
                Guild guild = null;
                if (!_languages.TryGetValue(guildChannel.GuildId, out language))
                {
                    guild = await ctx.GetOrCreateGuildAsync(guildChannel.GuildId);
                    _languages[guild.Id] = language = guild.Language;
                }

                if (!_guildPrefixes.TryGetValue(guildChannel.GuildId, out var customPrefixes))
                {
                    guild ??= await ctx.GetOrCreateGuildAsync(guildChannel.GuildId);
                    _guildPrefixes[guild.Id] = customPrefixes = guild.CustomPrefixes;
                }

                prefixes.AddRange(customPrefixes);
            }
            else if (!_languages.TryGetValue(message.Author.Id, out language))
            {
                var user = await ctx.GetOrCreateGlobalUserAsync(message.Author.Id);
                _languages[user.Id] = language = user.Language;
            }

            if (!CommandUtilities.HasAnyPrefix(message.Content, prefixes, StringComparison.OrdinalIgnoreCase,
                out var prefix, out var input)) return;

            var context = new AdminCommandContext(message, prefix, language, _provider);
            IResult result;

            if (!context.IsPrivate && await ctx.CommandAliases
                    .FirstOrDefaultAsync(x => x.GuildId == context.Guild.Id &&
                                            input.StartsWith(x.Alias,
                                            StringComparison.InvariantCultureIgnoreCase)) is { } alias)
            {
                result = await _commands.ExecuteAsync(alias, input, context);
            }
            else
            {
                result = await _commands.ExecuteAsync(input, context);
            }

            if (!(result is FailedResult failedResult)) return;

            var errorEmbed = await BuildErrorEmbedAsync(context, failedResult);
            if (!(errorEmbed is null))
                await context.Channel.SendMessageAsync(embed: errorEmbed);
        }

        public async Task HandleAsync(CommandExecutedEventArgs args)
        {
            var context = (AdminCommandContext) args.Context;
            var result = (AdminCommandResult) args.Result;

            await _logging.LogInfoAsync(
                $"User {context.User.Tag} executed command '{args.Result.Command.FullAliases[0]}' successfully after {result.ExecutionTime.TotalMilliseconds:F}ms",
                "CommandHandler");

            // TODO: Log stuff
            if (result.Attachment is { })
            {
                using (result.Attachment)
                {
                    await context.Channel.SendMessageAsync(result.Attachment, result.Text ?? string.Empty,
                        embed: result.Embed);
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(result.Text) || !(result.Embed is null))
            {
                await context.Channel.SendMessageAsync(result.Text ?? string.Empty, embed: result.Embed);
            }

            if (!context.IsPrivate)
            {
                using var ctx = new AdminDatabaseContext(_provider);
                var channel = await ctx.GetOrCreateTextChannelAsync(context.Guild.Id, context.Channel.Id);
                if (channel.Settings.HasFlag(TextChannelSettings.DeleteCommandMessages))
                {
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5));
                        await context.Message.DeleteAsync();
                    });
                }
            }
        }

        public async Task HandleAsync(CommandExecutionFailedEventArgs args)
        {
            var context = (AdminCommandContext) args.Context;

            await _logging.LogErrorAsync(
                $"User {context.User.Tag} caused command '{args.Result.Command.FullAliases[0]}' to to generate an exception.",
                "CommandHandler");
            await _logging.LogErrorAsync($"Step: {args.Result.CommandExecutionStep.Humanize(LetterCasing.Title)}",
                "CommandHandler");
            await _logging.LogErrorAsync(args.Result.Exception, "CommandHandler");
        }

        public override async Task InitializeAsync()
        {
            var modules = _commands.AddModules(Assembly.GetEntryAssembly(), action: builder =>
            {
                foreach (var command in CommandUtilities.EnumerateAllCommands(builder))
                {
                    command.AddCheck(new RequirePermissionsAttribute());

                    if (!command.Attributes.OfType<CooldownAttribute>().Any())
                        command.AddAttribute(new MinimumCooldownAttribute());

                    if (command.RunMode == RunMode.Parallel)
                        command.AddCheck(new NotExecutingAttribute());
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
            _commands.AddTypeParser(new BackpackUserParser());
            _commands.AddTypeParser(new SanitaryStringParser());
            _commands.AddTypeParser(new JumpLinkParser());

            // TODO: A better place to put this? A better way to do this?
            var set = new HashSet<string>();
            foreach (var command in _commands.GetAllCommands())
            {
                set.Add($"info_command_{command.FullAliases[0].Replace(' ', '_')}");
            }

            foreach (var module in _commands.TopLevelModules)
            {
                set.Add($"info_modules_{module.Name.ToLowerInvariant()}");
            }

            var language =
                JsonConvert.DeserializeObject<LocalizedLanguage>(
                    await File.ReadAllTextAsync("./Data/Responses/en-US.json"));
            var responses = language.Responses.ToDictionary(x => x.Key, x => x.Value);
            foreach (var key in set)
            {
                if (responses.TryAdd(key, new[] { "" }.ToImmutableArray()))
                {
                    await _logging.LogDebugAsync($"Added key {key}.", "CommandHandler");
                }
            }
            language.UpdateResponses(responses);
            await File.WriteAllTextAsync("./Data/Responses/en-US.json", JsonConvert.SerializeObject(language, Formatting.Indented));

            await _logging.LogInfoAsync($"{modules.Sum(x => CommandUtilities.EnumerateAllCommands(x).Count())} commands registered.",
                "CommandHandler");
        }

        private async Task<LocalEmbed> BuildErrorEmbedAsync(AdminCommandContext context, FailedResult failedResult)
        {
            if (!context.IsPrivate && !(failedResult is CommandOnCooldownResult))
            {
                using var ctx = new AdminDatabaseContext(_provider);
                var channel = await ctx.GetOrCreateTextChannelAsync(context.Guild.Id, context.Channel.Id);
                if (!channel.Settings.HasFlag(TextChannelSettings.SendCommandErrors))
                    return null;
            }

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
                        builder.AppendNewline(Markdown.CodeBlock($"{fullString}\n{"↑".PadLeft(center + 2)}"));
                    }
                    builder.AppendNewline(parserResult.Failure.GetValueOrDefault() switch
                    {
                        DefaultArgumentParserFailure.TooFewArguments => context.Localize("commanderror_toofewarguments",
                            argumentResult.Command.Parameters.Count(x => !x.IsOptional)),
                        DefaultArgumentParserFailure.TooManyArguments => context.Localize("commanderror_toomanyarguments",
                            argumentResult.Command.Parameters.Count),
                        // TODO: Localize the rest of the errors.
                        _ => argumentResult.Reason
                    });
                    break;
                case ChecksFailedResult checkResult when checkResult.FailedChecks.Any(x => x.Check is RequireOwnerAttribute):
                    return null;
                case ChecksFailedResult checkResult:
                    var failedChecks = checkResult.FailedChecks.ToList();
                    if (failedChecks.Count == 1 &&
                        failedChecks.Select(x => x.Check).FirstOrDefault() is NotExecutingAttribute)
                    {
                        return null;
                    }
                    else
                    {
                        failedChecks.RemoveAll(x => x.Check is NotExecutingAttribute);
                    }

                    builder.AppendNewline(context.Localize("commanderror_checks",
                        string.Join('\n', failedChecks.Select(x => x.Result.Reason))));
                    break;
                case CommandNotFoundResult _:
                    return null;
                case CommandOnCooldownResult cooldownResult:
                    _ = context.User.SendMessageAsync(embed: new LocalizedEmbedBuilder(_localization, context.Language)
                        .WithErrorColor()
                        .WithLocalizedTitle("commanderror")
                        .WithLocalizedDescription("commanderror_cooldown", cooldownResult.Cooldowns[0].RetryAfter
                            .HumanizeFormatted(_localization, context.Language, TimeUnit.Second))
                        .Build());
                    return null;
                case ExecutionFailedResult execResult:
                    await _logging.LogErrorAsync(execResult.Exception, "CommandHandler");
                    var frames = new StackTrace(execResult.Exception, true).GetFrames();
                    var frame = frames.First(x => x.GetFileName()?.Contains("Administrator") == true);
                    var message = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                        $"{execResult.Exception.Message} - at {frame.GetFileName()}, line {frame.GetFileLineNumber()} - {DateTimeOffset.UtcNow:g} UTC"));
                    builder.AppendNewline(context.Localize("commanderror_exception",
                        ConfigurationService.SUPPORT_GUILD, Markdown.CodeBlock(message)));
                    break;
                case OverloadsFailedResult overloadResult:
                    return await BuildErrorEmbedAsync(context,
                        overloadResult.FailedOverloads.Values.FirstOrDefault(x => !(x is ArgumentParseFailedResult)) ??
                        overloadResult.FailedOverloads.Values.First());
                case ParameterChecksFailedResult paramCheckResult:
                    builder.AppendNewline(context.Localize("commanderror_paramchecks",
                        string.Join('\n', paramCheckResult.FailedChecks.Select(x => x.Result.Reason))));
                    break;
                case TypeParseFailedResult typeParseResult:
                    builder.AppendNewline(Markdown.Code(
                            $"{context.Prefix}{string.Join(' ', context.Path)}{typeParseResult.Parameter.Command.FormatArguments()}"))
                        .AppendNewline($"\n{Markdown.Code(typeParseResult.Parameter.Name)}: {typeParseResult.Reason}");
                    break;
            }

            var value = builder.ToString();
            return !string.IsNullOrWhiteSpace(value)
                ? new LocalEmbedBuilder().WithErrorColor().WithTitle(context.Localize("commanderror")).WithDescription(value).Build()
                : null;
        }
    }
}