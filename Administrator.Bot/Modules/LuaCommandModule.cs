using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Extensions.Interactivity.Menus.Prompt;
using Humanizer;
using Qmmands;

namespace Administrator.Bot;

[SlashGroup("lua-command")]
public sealed class LuaCommandModule(AdminDbContext db, AttachmentService attachments, LuaCommandService luaCommands)
    : DiscordApplicationGuildModuleBase
{
    [SlashCommand("set")]
    [Description("Creates or overwrites a custom Lua command for your server.")]
    public async Task<IResult> SetAsync(
        [Name("name")]
        [Description("The name of the command.")]
        [Maximum(Discord.Limits.ApplicationCommand.MaxNameLength)]
            string name,
        [Name("metadata")]
        [Description("The Lua metadata (description, options, etc.) for this command.")]
        [RequireAttachmentExtensions("lua")]
            IAttachment metadataAttachment, 
        [Name("command")]
        [Description("The Lua execution code for this command.")]
        [RequireAttachmentExtensions("lua")]
            IAttachment commandAttachment)
    {
        await Deferral();
        
        name = name.Kebaberize();
        
        var (metadataStream, _) = await attachments.GetAttachmentAsync(metadataAttachment);
        using var metadataReader = new StreamReader(metadataStream);
        var metadata = await metadataReader.ReadToEndAsync();

        var (commandStream, _) = await attachments.GetAttachmentAsync(commandAttachment);
        using var commandReader = new StreamReader(commandStream);
        var command = await commandReader.ReadToEndAsync();

        if (await db.LuaCommands.FindAsync(Context.GuildId, name) is { } luaCommand)
        {
            var view = new PromptView(x =>
                x.WithContent($"An existing command already exists with the name {Markdown.Code($"/{name}")}, and will be overwritten by this command."));

            await View(view);

            if (!view.Result)
                return Response("Action canceled.");

            db.LuaCommands.Remove(luaCommand);
            await db.SaveChangesAsync();
        }
        
        db.LuaCommands.Add(new LuaCommand(Context.GuildId, name, metadataStream.ToArray(), commandStream.ToArray()));
        await db.SaveChangesAsync();
        await luaCommands.ReloadLuaCommandsAsync(Context.GuildId);

        return Response($"Command {Markdown.Code($"/{name}")} created or updated!");
    }
}