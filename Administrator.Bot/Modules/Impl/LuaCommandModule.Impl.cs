using System.Text;
using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Utilities.Threading;
using Humanizer;
using Laylua;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qmmands;
using Qommon;

namespace Administrator.Bot;

public sealed partial class LuaCommandModule(AdminDbContext db, AttachmentService attachments, LuaCommandService luaCommands, AutoCompleteService autoComplete)
    : DiscordApplicationGuildModuleBase
{
    private const string METADATA_SEPARATOR = "-- END METADATA --";
    private const string INSERTED_RETURN = "return metadata";
    
    public partial async Task<IResult> Set(IAttachment commandAttachment, bool keepPersistence)
    {
        const string requiredMetadataDeclaration = "local metadata = ";
        const string example = $$"""
                                 -- metadata goes here
                                 local metadata = { name = "example" }
                                 
                                 {{METADATA_SEPARATOR}}
                                 
                                 -- command goes here
                                 return "Hello from Lua!"
                                 """;
        
        await Deferral();
        
        var (stream, _) = await attachments.GetAttachmentAsync(commandAttachment);
        using var reader = new StreamReader(stream);
        var raw = await reader.ReadToEndAsync();
        // CRLF -> LF
        raw = raw.ReplaceLineEndings("\n");

        var split = raw.Split(METADATA_SEPARATOR, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (split.Length <= 1)
        {
            return Response($"Your lua command file must follow the following structure:\n{Markdown.CodeBlock("lua", example)}\n" +
                            $"(Make sure to include the separator ({Markdown.Code(METADATA_SEPARATOR)}) {Markdown.Bold("between")} the metadata and the command!)");
        }
        
        using var cts = new Cts();
        using var lua = new Lua();
        lua.OpenDiscordLibraries(Context, cts.Token);
        lua.OpenLibrary(LuaLibraries.Standard.Math);
        lua.OpenLibrary(LuaLibraries.Standard.String);

        // just SHOOOVE a `return metadata` in there to force return the metadata object
        var rawMetadata = split[0].Replace(INSERTED_RETURN, "\n") + $"\n{INSERTED_RETURN}";
        if (!rawMetadata.Contains(requiredMetadataDeclaration))
        {
            return Response("Your command's metadata must contain a definition for the metadata object, specifically a line starting with " +
                            $"{Markdown.Code(requiredMetadataDeclaration)}.");
        }

        string commandName;
        LuaSlashCommand slashCommand;
        try
        {
            var rawCommand = lua.Evaluate<LuaTable>(rawMetadata);
            Guard.IsNotNull(rawCommand);
            slashCommand = new LuaSlashCommand(rawCommand);
            slashCommand.Validate();
            commandName = slashCommand.Name;
        }
        catch (Exception ex)
        {
            return Response("Failed to evaluate your lua command's metadata for conversion to a slash command.\n" +
                            "Ensure that there are no syntax errors, or see the below message for additional information.\n" +
                            Markdown.CodeBlock(ex.Message));
        }

        AdminPromptView view;

        var guild = await db.Guilds.GetOrCreateAsync(Context.GuildId);
        var metadataChanged = true;
        if (await db.LuaCommands.FindAsync(Context.GuildId, commandName) is { } luaCommand)
        {
            var metadataTable = luaCommand.ToMetadataTable(lua, Bot);
            var currentSlashCommand = new LuaSlashCommand(metadataTable);

            if (slashCommand.Equals(currentSlashCommand))
            {
                metadataChanged = false;
            }
            
            view = new AdminPromptView($"An existing command already exists with the name {Markdown.Code($"/{commandName}")}, and will be overwritten by this command.",
                    slashCommand.ToDisplayEmbed());
        }
        else if (await db.LuaCommands.CountAsync(x => x.GuildId == Context.GuildId) > guild.MaxLuaCommands)
        {
            return Response($"This server is only allowed to create up to {"lua command".ToQuantity(guild.MaxLuaCommands)}.");
        }
        else
        {
            luaCommand = null;
            view = new AdminPromptView($"A new custom slash command {Markdown.Code($"/{commandName}")} will be created.",
                slashCommand.ToDisplayEmbed());
        }
        
        await View(view.OnConfirm($"Command {Markdown.Code($"/{commandName}")} created or updated!\n" +
                                  $"(It may take up to 30 seconds to properly show up.)"));

        if (view.Result)
        {
            byte[] persistence = [];
            if (luaCommand is not null)
            {
                if (keepPersistence)
                    persistence = luaCommand.Persistence;
                
                db.LuaCommands.Remove(luaCommand);
                await db.SaveChangesAsync();
            }

            var commandRemainder = string.Join("\n", split[1..]);
            var metadataBytes = Encoding.Default.GetBytes(rawMetadata).GZipCompress();
            var commandBytes = Encoding.Default.GetBytes(commandRemainder).GZipCompress();
            var command = new LuaCommand(Context.GuildId, commandName, metadataBytes, commandBytes)
            {
                Persistence = persistence
            };

            db.LuaCommands.Add(command);
            await db.SaveChangesAsync();

            await luaCommands.ReloadLuaCommandsAsync(Context.GuildId, metadataChanged);
            
            if (!metadataChanged)
            {
                Logger.LogDebug("Lua slash command {Command} in guild {GuildId} detected no metadata changes, skipping reload.", slashCommand.Name,
                    Context.GuildId.RawValue);
            }
        }

        return default!;
    }

    public partial async Task Remove(string commandName, bool includeData)
    {
        if (await db.LuaCommands.FirstOrDefaultAsync(x => x.GuildId == Context.GuildId && x.Name == commandName) is not { } command)
        {
            await Response("No Lua command could be found with that name!").AsEphemeral();
            return;
        }

        var view = new AdminPromptView($"The custom Lua command {Markdown.Code($"/{commandName}")} will be permanently removed.")
            .OnConfirm($"Custom Lua command {Markdown.Code($"/{commandName}")} removed.");

        await View(view);

        if (!view.Result)
            return;

        db.LuaCommands.Remove(command);
        await db.SaveChangesAsync();
        await luaCommands.ReloadLuaCommandsAsync(Context.GuildId);

        if (!includeData)
            return;

        var stream = new MemoryStream();
        await using var writer = new StreamWriter(stream, leaveOpen: true);
        writer.AutoFlush = true;
        writer.NewLine = "\n";

        await writer.WriteLineAsync(Encoding.Default.GetString(command.Metadata.GZipDecompress()).Replace(INSERTED_RETURN, string.Empty));
        await writer.WriteLineAsync();
        await writer.WriteLineAsync(METADATA_SEPARATOR);
        await writer.WriteLineAsync();
        await writer.WriteLineAsync(Encoding.Default.GetString(command.Command.GZipDecompress()));

        stream.Seek(0, SeekOrigin.Begin);
        
        await Response(new LocalInteractionMessageResponse().AddAttachment(new LocalAttachment(stream, $"{commandName}.lua")));
    }

    public partial async Task AutoCompleteLuaCommands(AutoComplete<string> commandName)
    {
        if (!commandName.IsFocused)
            return;
        
        var commands = await db.LuaCommands.Where(x => x.GuildId == Context.GuildId).ToListAsync();
        autoComplete.AutoComplete(commandName, commands);
    }
}