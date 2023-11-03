using Disqord;
using Disqord.Bot;
using Disqord.Bot.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qmmands;
using Qmmands.Default;

namespace Administrator.Bot;

public sealed class AdministratorBot(IOptions<DiscordBotConfiguration> options, ILogger<DiscordBot> logger, IServiceProvider services, DiscordClient client)
    : DiscordBot(options, logger, services, client)
{
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
        return base.AddTypeParsers(typeParserProvider, cancellationToken);
    }
    
    protected override ValueTask OnFailedResult(IDiscordCommandContext context, IResult result)
    {
        Logger.LogInformation(result.FailureReason!); // TODO: remove
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
}