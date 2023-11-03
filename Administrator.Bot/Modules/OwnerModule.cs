using System.Globalization;
using Administrator.Database;
using Disqord;
using Disqord.Bot;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Humanizer;
using Laylua;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using IResult = Qmmands.IResult;

namespace Administrator.Bot;

[SlashGroup("owner")]
[RequireBotOwner]
public sealed class OwnerModule(AdminDbContext db, AttachmentService attachments) : DiscordApplicationGuildModuleBase
{
    [MutateModule]
    public static void MutateModule(DiscordBotBase bot, IModuleBuilder module)
    {
        var guildId = bot.Services.GetRequiredService<IConfiguration>().GetValue<ulong>("OwnerModuleGuildId");
        module.Checks.Add(new RequireGuildAttribute(guildId));
    }

    [SlashCommand("lua")]
    public async Task LuaAsync(string? code = null, [RequireAttachmentExtensions("lua")] IAttachment? luaFile = null)
    {
        await Deferral();
        using var lua = new Lua(CultureInfo.CurrentCulture);

        lua.OpenDiscordLibraries(Context);

        if (luaFile is not null)
        {
            var (stream, _) = await attachments.GetAttachmentAsync(luaFile);

            using var reader = new StreamReader(stream);

            code = await reader.ReadToEndAsync();
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            await Response("No code????");
            return;
        }

        try
        {
            var msg = lua.Evaluate<LuaTable>(code);
            if (msg is not null)
                await Response(DiscordLuaLibraryBase.ConvertMessage<LocalInteractionMessageResponse>(msg));
            else
                await Response("Done");
        }
        catch (LuaException ex)
        {
            await Response($"failed: {ex.Message}");
        }
    }

    /*
    [SlashCommand("clear-cache")]
    [Description("Clear's the bot's internal cache.")]
    public IResult ClearCache()
    {
        _cache.Compact(1.0);
        Logger.LogWarning("!!!CACHE RESET!!!");
        return Response("Cache cleared!");
    }
    */

    [SlashCommand("list-permissions")]
    [Description("Lists all required bot permissions from all commands.")]
    public IResult ListPermissions()
    {
        return Response(Bot.Commands.GetRequiredBotPermissions().Humanize(LetterCasing.Title));
    }

    /*
    [SlashCommand("announce-all")]
    [Description("Sends an announcement to all servers.")]
    public async Task<IResult> AnnounceAllAsync(
        [Description("A markdown (md) or text (txt) file to send.")]
        [RequireAttachmentExtensions("md", "txt")]
            IAttachment announcement)
    {
        using var file = await _attachments.GetAttachmentAsync(announcement);
        using var reader = new StreamReader(file.Stream.Value);

        var content = await reader.ReadToEndAsync();
        
        var prompt = new PromptView(x => x.WithContent(content));
        await View(prompt);

        if (!prompt.Result)
            return Response("Operation canceled.");

        var output = new MemoryStream();
        await using var writer = new StreamWriter(output, leaveOpen: true);

        var sentOwnerIds = new HashSet<Snowflake>();
        foreach (var guild in Bot.GetGuilds().Values)
        {
            var loggingChannel = await _db.GetLoggingChannelAsync(guild.Id, LogEventType.BotAnnouncements);
        }
    }
    */
}