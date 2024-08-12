using System.Reflection;
using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Bot;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Disqord.Bot.Commands.Interaction;
using Disqord.Gateway;
using Disqord.Http;
using Disqord.Rest;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qmmands;
using Qmmands.Default;
using Qommon.Metadata;

namespace Administrator.Bot;

public sealed class AdministratorBot(IOptions<DiscordBotConfiguration> options, ILogger<DiscordBot> logger, IServiceProvider services, DiscordClient client)
    : DiscordBot(options, logger, services, client), IDiscordEntityRequester
{
#if !MIGRATING
    protected override async ValueTask<IResult> OnBeforeExecuted(IDiscordCommandContext context)
    {
        var baseResult = await base.OnBeforeExecuted(context);
        await using var scope = context.Bot.Services.CreateAsyncScopeWithDatabase(out var db);

        await db.Users.GetOrCreateAsync(context.AuthorId);

        if (context.GuildId is { } guildId)
        {
            await db.Members.GetOrCreateAsync(guildId, context.AuthorId);
        }

        return baseResult;
    }
#endif
    
    protected override IEnumerable<Assembly> GetModuleAssemblies()
        => [GetType().Assembly];

    protected override async ValueTask InitializeModules(CancellationToken cancellationToken)
    {
        var luaCommandService = Services.GetRequiredService<LuaCommandService>();
        await using var scope = Services.CreateAsyncScopeWithDatabase(out var db);
        var luaCommands = await db.LuaCommands.ToListAsync(cancellationToken);

        foreach (var guildId in luaCommands.DistinctBy(x => x.GuildId).Select(x => x.GuildId))
        {
            await luaCommandService.ReloadLuaCommandsAsync(guildId);
        }

        await base.InitializeModules(cancellationToken);
    }

    protected override ValueTask AddTypeParsers(DefaultTypeParserProvider typeParserProvider, CancellationToken cancellationToken)
    {
        typeParserProvider.AddParser(new TimeSpanTypeParser());
        typeParserProvider.AddParser(new EmojiTypeParser());
        typeParserProvider.AddParser(new TimeZoneInfoTypeParser());
        typeParserProvider.AddParser(new DateTimeOffsetTypeParser());
        typeParserProvider.AddParser(new BackpackUserTypeParser());
        typeParserProvider.AddParser(new TagTypeParser());
        return base.AddTypeParsers(typeParserProvider, cancellationToken);
    }

    protected override async ValueTask OnCommandResult(IDiscordCommandContext context, IDiscordCommandResult result)
    {
        try
        {
            await base.OnCommandResult(context, result);
        }
        catch (RestApiException ex) when (ex.StatusCode is HttpResponseStatusCode.BadRequest)
        {
            // this should realistically only happen if the command returns an invalid response body.
            var interactionContext = (IDiscordInteractionCommandContext)context;
            var mentions = Services.GetRequiredService<SlashCommandMentionService>();
            var commandName = mentions.GetMention(context.Command!) ?? Markdown.Code($"/{context.Command!.Name}");
            
            await interactionContext.Interaction.RespondOrFollowupAsync(new LocalInteractionMessageResponse()
                .WithContent($"The command {commandName} failed to execute due to returning an invalid response body to Discord. " +
                             "If this is not your own custom Lua command, please report the command name and error to a developer:\n" +
                             Markdown.CodeBlock(ex.Message)));
        }
    }

    protected override ValueTask OnFailedResult(IDiscordCommandContext context, IResult result)
    {
#if DEBUG
        if (result is not CommandNotFoundResult)
            Logger.LogInformation("{Type}: {Message}", result.GetType().Name, result.FailureReason);
#endif
        return base.OnFailedResult(context, result);
    }

    protected override string? FormatFailureReason(IDiscordCommandContext context, IResult result)
    {
        if (result is ExceptionResult exceptionResult)
        {
            return "An unhandled exception has occurred:\n" +
                   Markdown.CodeBlock($"{exceptionResult.Exception.GetType()}: {exceptionResult.Exception.Message}");
        }

        return base.FormatFailureReason(context, result);
    }

    protected override ValueTask OnInitialize(CancellationToken cancellationToken)
    {
        if (Services.GetRequiredService<ICommandPipelineProvider>() is DefaultCommandPipelineProvider defaultCommandPipelineProvider)
        {
            var autoCompletePipeline = defaultCommandPipelineProvider.First(pipeline => pipeline is DefaultBotCommandsSetup.AutoCompleteCommandPipeline);
            var autoCompleteSwitchIndex = autoCompletePipeline.IndexOf(autoCompletePipeline.First(step => step is DefaultApplicationExecutionSteps.AutoCompleteSwitch));
            autoCompletePipeline.Insert(autoCompleteSwitchIndex, new DelegateCommandExecutionStep((context, step) =>
            {
                context.SetMetadata("OriginalCommand", context.Command);
                return step.ExecuteAsync(context);
            }));
        }

        return base.OnInitialize(cancellationToken);
    }

    Task<IDirectChannel> IDiscordEntityRequester.FetchDirectChannelAsync(Snowflake userId)
        => this.CreateDirectChannelAsync(userId);

    IUser? IDiscordEntityRequester.GetUser(Snowflake userId)
        => this.GetUser(userId);

    IMember? IDiscordEntityRequester.GetMember(Snowflake guildId, Snowflake memberId)
        => this.GetMember(guildId, memberId);

    IRole? IDiscordEntityRequester.GetRole(Snowflake guildId, Snowflake roleId)
        => this.GetRole(guildId, roleId);

    IGuildChannel? IDiscordEntityRequester.GetChannel(Snowflake guildId, Snowflake channelId)
        => this.GetChannel(guildId, channelId);
}