using System.Text;
using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Bot;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Humanizer;
using LinqToDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Qmmands;

namespace Administrator.Bot;

#if DEBUG
[SlashGroup("owner")]
[RequireBotOwner(Group = "1")]
[RequireInitialAuthorPermissions(Permissions.Administrator, Group = "1")]
public sealed class OwnerModule(SlashCommandMentionService mentions, AdminDbContext db) : DiscordApplicationGuildModuleBase
{
    [MutateModule]
    public static void MutateModule(DiscordBotBase bot, IModuleBuilder module)
    {
        var config = bot.Services.GetRequiredService<IOptions<AdministratorBotConfiguration>>().Value;
        var guildId = config.OwnerModuleGuildId;
        module.Checks.Add(new RequireGuildAttribute(guildId));
    }

    [SlashCommand("fix-decay")]
    public async Task<IResult> FixDemeritPointsAsync()
    {
        await Deferral();

        var entries = await db.Members.Where(x => x.NextDemeritPointDecay != null)
            .Select(member => new
            {
                Member = member,
                Guild = db.Guilds.FirstOrDefault(x => x.GuildId == member.GuildId)
            })
            .ToListAsync();

        foreach (var entry in entries)
        {
            if (entry.Guild?.DemeritPointsDecayInterval is not { } decayInterval)
                continue;

            entry.Member.NextDemeritPointDecay += decayInterval;
        }

        var count = await db.SaveChangesAsync();
        return Response($"{count} warnings updated.");
    }

    [SlashCommand("generate-commandlist")]
    public async Task<IResult> GenerateCommandListAsync()
    {
        await Deferral();

        var builder = new StringBuilder()
            .AppendNewline("|Command|Description|")
            .AppendNewline("|---|---|");
        
        foreach (var command in Bot.Commands.EnumerateApplicationModules().SelectMany(x => x.Commands).Where(x => x.Type is ApplicationCommandType.Slash))
        {
            builder.Append($"|{Markdown.Code($"/{SlashCommandMentionService.GetPath(command)}")}|{command.Description}");

            if (command.Checks.Concat(command.Module.Checks).OfType<RequireInitialAuthorPermissionsAttribute>().FirstOrDefault() is { Permissions: var requiredPermission })
            {
                builder.Append("<br>").Append($"{Markdown.Bold("Required Permissions:")} {requiredPermission.Humanize(LetterCasing.Title)}");
            }

            builder.AppendNewline("|");
        }

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(builder.ToString()));
        return Response(new LocalInteractionMessageResponse().AddAttachment(new LocalAttachment(stream, "commands.md")));
    }

#if MIGRATING
    [SlashCommand("migrate-database")]
    public async Task MigrateAsync()
    {
        await Deferral();
        var services = new ServiceCollection()
            .AddDbContext<OldAdminDbContext>()
            .BuildServiceProvider();

        await using var scope = services.CreateAsyncScope();
        var oldDb = scope.ServiceProvider.GetRequiredService<OldAdminDbContext>();

        var bot = (AdministratorBot)Bot;
        await foreach (var response in oldDb.MigrateAsync(db, bot, "415401238356819968"))
        {
            await Response(response);
        }
    }
#endif  

    [SlashGroup("xp")]
    public sealed class OwnerXpModule(AdminDbContext db) : DiscordApplicationGuildModuleBase
    {
        [SlashCommand("set")]
        public async Task<IResult> SetAsync(IMember member, int totalXp, bool resetGainTime = false)
        {
            var mb = await db.Members.GetOrCreateAsync(member.GuildId, member.Id);
            
            mb.TotalXp = totalXp;
            if (resetGainTime)
                mb.LastXpGain = DateTimeOffset.MinValue;
            
            await db.SaveChangesAsync();
            return Response("Done!");
        }
    }

    [SlashCommand("dump-commands")]
    public async Task<IResult> DumpCommandsAsync()
    {
        var builder = new StringBuilder();

        foreach (var command in mentions.CommandMap.Keys.Order())
        {
            builder.AppendNewline($"/{command}");
        }
        
        var output = new MemoryStream();
        await using var writer = new StreamWriter(output, leaveOpen: true) { AutoFlush = true };
        await writer.WriteAsync(builder);

        output.Seek(0, SeekOrigin.Begin);
        return Response(new LocalInteractionMessageResponse().AddAttachment(new LocalAttachment(output, "cmds.txt")));
    }

    [SlashCommand("list-permissions")]
    [Description("Lists all required bot permissions from all commands.")]
    public IResult ListPermissions()
    {
        var permissions = Bot.Commands.GetRequiredBotPermissions();
        var extraRequiredPermissions = InitialJoinService.ExtraRequiredPermissions.Keys.Aggregate(Permissions.None, (curr, p) => curr | p);
        permissions |= extraRequiredPermissions;
        return Response(permissions.Humanize(LetterCasing.Title));
    }
}
#endif