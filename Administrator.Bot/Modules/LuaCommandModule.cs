using Disqord;
using Disqord.Bot.Commands.Application;
using Qmmands;

namespace Administrator.Bot;

[SlashGroup("lua-command")]
[RequireInitialAuthorPermissions(Permissions.ManageGuild)]
public sealed partial class LuaCommandModule
{
    [SlashCommand("set")]
    [Description("Creates or overwrites a custom Lua command for this server.")]
    public partial Task<IResult> Set(
        [Name("command")] [Description("A lua file describing the command and its metadata.")]
        [RequireAttachmentExtensions("lua")]
            IAttachment commandAttachment,
        [Description("If True and replacing an existing command, keep persistent data. Default: True")]
            bool keepPersistence = true);

    [SlashCommand("remove")]
    [Description("Removes an existing custom Lua command from this server.")]
    public partial Task Remove(
        [Description("The name of the command to remove.")]
            string commandName,
        [Description("Whether to return the original submitted Lua file back. Default: False")]
            bool includeData = false);

    [AutoComplete("remove")]
    public partial Task AutoCompleteLuaCommands(AutoComplete<string> commandName);
}