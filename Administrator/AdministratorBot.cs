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

        protected override LocalMessageBuilder FormatFailureMessage(DiscordCommandContext context, FailedResult result)
        {
            if (result is CommandNotFoundResult)
                return null;
            
            var commandPath = string.Join(' ', context.Path);
            var descriptionBuilder = new StringBuilder();
            var builder = new LocalMessageBuilder()
                .WithEmbed(new LocalEmbedBuilder()
                    .WithErrorColor()
                    .WithTitle("Command Error:"));

            switch (result)
            {
                case ArgumentParseFailedResult {ParserResult: DefaultArgumentParserResult argParserResult} argumentResult:
                    if (argParserResult.FailurePosition.HasValue)
                    {
                        var center = commandPath.Length + argParserResult.FailurePosition.Value;
                        var fullString = $"{commandPath} {argumentResult.RawArguments}".FixateTo(ref center, 30 - commandPath.Length);
                        descriptionBuilder.AppendNewline(Markdown.CodeBlock($"{fullString}\n{"↑".PadLeft(center + 2)}"));
                    }
                    
                    var valueBuilder = new StringBuilder();
                    var fieldBuilder = new LocalEmbedFieldBuilder()
                        .WithName(Markdown.Code($"{commandPath}{argumentResult.Command.FormatArguments()}"));
                    
                    switch (argParserResult.Failure)
                    {
                        case DefaultArgumentParserFailure.TooFewArguments:
                            var missingParameters = argParserResult.EnumerateMissingParameters().ToList();

                            if (missingParameters.Count == 1)
                                valueBuilder.AppendNewline($"The required parameter {Markdown.Code(missingParameters[0].Name)} is missing.");
                            
                            else
                            {
                                valueBuilder.Append("The required parameters ");
                                foreach (var missingParameter in argParserResult.EnumerateMissingParameters())
                                {
                                    valueBuilder.Append(Markdown.Code(missingParameter.Name))
                                        .Append(' ');
                                }

                                valueBuilder.AppendNewline(" are missing.");
                            }
                            break;
                        case DefaultArgumentParserFailure.TooManyArguments:
                            valueBuilder.Append("You supplied too many arguments for this command; ");
                            if (argumentResult.Command.Parameters.Any(x => x.IsOptional))
                            {
                                valueBuilder.AppendNewline($"it expects {argumentResult.Command.Parameters.Count} or less.");
                            }
                            else
                            {
                                valueBuilder.AppendNewline($"it expects {argumentResult.Command.Parameters.Count}.");
                            }
                            break;
                        default:
                            valueBuilder.AppendNewline(argParserResult.FailureReason);
                            break;
                    }

                    builder.Embed.AddField(fieldBuilder.WithValue(valueBuilder.ToString()));
                    break;
                case ChecksFailedResult checkResult when checkResult.FailedChecks.Any(x => x.Check is RequireBotOwnerAttribute):
                    return null;
                case ChecksFailedResult checkResult:
                    var failedChecks = checkResult.FailedChecks.ToList();
                    
                    // TODO: Remove checks
                    if (failedChecks.Count == 0)
                        return null;

                    descriptionBuilder.AppendNewline("One or more requirements for this command were not met:")
                        .Append(string.Join('\n', failedChecks.Select(x => x.Result.FailureReason)));
                    break;
                case CommandExecutionFailedResult executionResult:
                    Logger.LogError(executionResult.Exception, 
                        "An exception was thrown executing the command {Command}. Context: {@Context}",
                        commandPath, context);

                    var config = Services.GetRequiredService<IConfiguration>();

                    descriptionBuilder.AppendNewline("An unhandled exception was thrown attempting to run this command.")
                        .Append("Please report the following ID to the support channel in ")
                        .AppendNewline(Markdown.Link($"{config["SupportServer:Name"]}:", 
                            $"https://discord.gg/{config["SupportServer:Code"]}"))
                        .AppendNewline(Markdown.CodeBlock(context.Message.Id.ToString()));
                    break;
                case OverloadsFailedResult overloadResult:
                    if (overloadResult.FailedOverloads.Count == 1)
                    {
                        return FormatFailureMessage(context, overloadResult.FailedOverloads.First().Value);
                    }

                    foreach (var (overload, failedResult) in overloadResult.FailedOverloads)
                    {
                        var overloadFieldBuilder = new LocalEmbedFieldBuilder()
                            .WithName(Markdown.Code($"{commandPath}{overload.FormatArguments()}"));

                        var formatMessage = FormatFailureMessage(context, failedResult);

                        if (!string.IsNullOrWhiteSpace(formatMessage?.Embed?.Description))
                        {
                            builder.Embed.AddField(overloadFieldBuilder.WithValue(formatMessage.Embed.Description));
                        }
                    }

                    descriptionBuilder.AppendNewline("All possible variations of this command were unable to run.")
                        .AppendNewline("Check the below errors for more detailed information on why this may be the case.");
                    break;
                case ParameterChecksFailedResult parameterResult:
                    descriptionBuilder.AppendNewline("One or more requirements for the parameters in this command were not met:")
                        .AppendNewline(string.Join('\n', parameterResult.FailedChecks.Select(x => x.Result.FailureReason)));
                    break;
                case TypeParseFailedResult typeParserResult:
                    if (string.IsNullOrWhiteSpace(typeParserResult.FailureReason))
                        return null;

                    descriptionBuilder.AppendNewline(
                            Markdown.CodeBlock($"{commandPath}{typeParserResult.Parameter.Command.FormatArguments()}"))
                        .AppendNewline($"{Markdown.Code(typeParserResult.Parameter.Name)}: {typeParserResult.FailureReason}");
                    break;
                default:
                    return null;
            }

            if (descriptionBuilder.Length > 0)
                builder.Embed.WithDescription(descriptionBuilder.ToString());

            return builder;
        }

        protected override async ValueTask HandleFailedResultAsync(DiscordCommandContext context, FailedResult result)
        {
            // TODO: Don't send message per text channel settings
            
            try
            {
                await base.HandleFailedResultAsync(context, result);
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
                    _ = context.Author.SendMessageAsync(new LocalMessageBuilder().WithContent(
                            $"Hello. I attempted to reply to your command in {guildContext.Channel.Mention}, but I don't have permissions to send messages.\n" +
                            "Please contact a moderator so they can grant me proper permissions to reply to commands.")
                        .Build());
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