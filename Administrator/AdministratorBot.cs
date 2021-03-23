using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Administrator.Commands;
using Administrator.Database;
using Administrator.Extensions;
using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using Disqord.Rest;
using Disqord.Rest.Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qmmands;

namespace Administrator
{
    public sealed class AdministratorBot : DiscordBot
    {
        public AdministratorBot(IOptions<DiscordBotConfiguration> options, ILogger<AdministratorBot> logger,
            IPrefixProvider prefixes, ICommandQueue queue, CommandService commands, IServiceProvider services,
            DiscordClient client)
            : base(options, logger, prefixes, queue, commands, services, client)
        { }

        protected override ValueTask AddTypeParsersAsync(CancellationToken cancellationToken = default)
        {
            Commands.AddTypeParser(new KeyedTypeParser<Punishment>());
            Commands.AddTypeParser(new EmojiTypeParser<IEmoji>());
            Commands.AddTypeParser(new EmojiTypeParser<ICustomEmoji>());
            Commands.AddTypeParser(new EmojiTypeParser<IGuildEmoji>());
            Commands.AddTypeParser(new SanitizedStringTypeParser());
            return base.AddTypeParsersAsync(cancellationToken);
        }

        protected override async ValueTask HandleFailedResultAsync(DiscordCommandContext context, FailedResult result)
        {
            // TODO: Don't send message per text channel settings
            
            if (result is CommandNotFoundResult)
                return;

            var embedBuilder = new LocalEmbedBuilder()
                .WithErrorColor()
                .WithTitle("Command Error");
            var builder = new StringBuilder();
            var fullPath = string.Join(' ', context.Path);
            
            switch (result)
            {
                case ArgumentParseFailedResult {ParserResult: DefaultArgumentParserResult parserResult} argumentResult:
                    if (parserResult.FailurePosition.HasValue)
                    {
                        var center = fullPath.Length + parserResult.FailurePosition.Value;
                        var fullString = $"{fullPath} {argumentResult.RawArguments}".FixateTo(ref center, 30 - fullPath.Length);
                        builder.AppendNewline(Markdown.CodeBlock($"{fullString}\n{"↑".PadLeft(center + 2)}"));
                    }

                    var field = new LocalEmbedFieldBuilder()
                        .WithName(Markdown.Code($"{fullPath}{parserResult.Command.FormatArguments()}"));

                    var valueBuilder = new StringBuilder();
                    

                    switch (parserResult.Failure)
                    {
                        case DefaultArgumentParserFailure.TooFewArguments:
                            var missingParameters = parserResult.EnumerateMissingParameters().ToList();

                            if (missingParameters.Count == 1)
                                valueBuilder.AppendNewline($"The required parameter {Markdown.Code(missingParameters[0].Name)} is missing.");
                            else
                            {
                                valueBuilder.Append("The required parameters ");
                                foreach (var missingParameter in parserResult.EnumerateMissingParameters())
                                {
                                    builder.Append(Markdown.Code(missingParameter.Name))
                                        .Append(' ');
                                }

                                valueBuilder.AppendNewline(" are missing.");
                            }
                            break;
                        case DefaultArgumentParserFailure.TooManyArguments:
                            valueBuilder.Append("You supplied too many arguments for this command; ");
                            if (parserResult.Command.Parameters.Any(x => x.IsOptional))
                            {
                                valueBuilder.AppendNewline($"it expects {parserResult.Command.Parameters.Count} or less.");
                            }
                            else
                            {
                                valueBuilder.AppendNewline($"it expects {parserResult.Command.Parameters.Count}.");
                            }
                            break;
                        default:
                            valueBuilder.AppendNewline(parserResult.FailureReason);
                            break;
                    }

                    embedBuilder.AddField(field.WithValue(valueBuilder.ToString()));
                    break;
                case ChecksFailedResult checkResult when checkResult.FailedChecks.Any(x => x.Check is RequireBotOwnerAttribute):
                    break;
                case ChecksFailedResult checkResult:
                    var failedChecks = checkResult.FailedChecks.ToList();
                    // TODO: Remove checks
                    if (failedChecks.Count == 0)
                        break;

                    builder.AppendNewline("One or more requirements for this command were not met:")
                        .Append(string.Join('\n', failedChecks.Select(x => x.Result.FailureReason)));
                    break;
                case ExecutionFailedResult executionResult:
                    Logger.LogError(executionResult.Exception, "An exception was thrown executing the command {Command}. Context: {@Context}",
                        fullPath, context);

                    builder.AppendNewline("An unhandled exception was thrown attempting to run this command.")
                        .Append("Please report the following ID to the support channel in ")
                        .AppendNewline(Markdown.Link("my support server:", Services.GetRequiredService<IConfiguration>()["SUPPORT_LINK"]))
                        .AppendNewline(Markdown.CodeBlock(context.Message.Id.ToString()));
                    break;
                case OverloadsFailedResult overloadsFailedResult:
                    if (overloadsFailedResult.FailedOverloads.Count == 1)
                    {
                        await HandleFailedResultAsync(context, overloadsFailedResult.FailedOverloads.First().Value);
                        return;
                    }
                    foreach (var overloadField in overloadsFailedResult.FailedOverloads.Select(x =>
                        new LocalEmbedFieldBuilder()
                            .WithName(Markdown.Code($"{fullPath}{x.Key.FormatArguments()}"))
                            .WithValue(x.Value.FailureReason)))
                    {
                        embedBuilder.AddField(overloadField);
                    }

                    builder.AppendNewline("All possible variations of this command were unable to run.")
                        .AppendNewline("Check the below errors for more detailed information on why this may be the case.");
                    break;
                case ParameterChecksFailedResult parameterResult:
                    builder.AppendNewline("One or more requirements for the parameters in this command were not met:")
                        .AppendNewline(string.Join('\n', parameterResult.FailedChecks.Select(x => x.Result.FailureReason)));
                    break;
                case TypeParseFailedResult typeParseResult:
                    if (string.IsNullOrWhiteSpace(typeParseResult.FailureReason))
                        break;

                    builder.AppendNewline(Markdown.CodeBlock($"{fullPath}{context.Command.FormatArguments()}"))
                        .AppendNewline($"{Markdown.Code(typeParseResult.Parameter.Name)}: {typeParseResult.FailureReason}");
                    break;
            }

            if (builder.Length == 0 && embedBuilder.Fields.Count == 0) return;
            
            try
            {
                await context.Bot.SendMessageAsync(context.ChannelId, new LocalMessageBuilder()
                    .WithEmbed(embedBuilder.WithDescription(builder.ToString()))
                    .Build());
            }
            catch (RestApiException ex) when (ex.ErrorModel.Code == RestApiErrorCode.CannotSendMessagesToThisUser)
            { }
            catch (RestApiException ex) when (ex.ErrorModel.Code == RestApiErrorCode.MissingPermissions &&
                                              context is DiscordGuildCommandContext guildContext)
            {
                var permissions = Discord.Permissions.CalculatePermissions(guildContext.Guild, guildContext.Channel,
                    guildContext.CurrentMember, guildContext.CurrentMember.GetRoles().Values);


                // Was it because we can't send messages at all?
                if (!permissions.Has(Permission.SendMessages))
                {
                    /* TODO: Wait for IUser#SendMessageAsync
                    _ = context.Author.SendMessageAsync(
                        $"Hello. I attempted to reply to your command in {channel.Mention}, but I don't have permissions to send messages.\n" +
                        "Please contact a moderator so they can grant me proper permissions to reply to commands.");
                    */
                }
                // Was it because we can't send embeds?
                else if (!permissions.Has(Permission.EmbedLinks))
                {
                    await context.Bot.SendMessageAsync(context.ChannelId,
                        new LocalMessageBuilder()
                            .WithContent("I was unable to reply to your command properly because I lack Embed Links permissions in this channel.")
                            .Build());
                }
            }
        }
    }
}