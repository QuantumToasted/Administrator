using Disqord.Bot.Commands.Application;
using Qmmands;

namespace Administrator.Bot;

public sealed partial class HelpModule
{
    [SlashCommand("help")]
    [Description("Displays help information related to the bot.")]
    public partial Task<IResult> Help(); // TODO: topics?

    [SlashCommand("mention-command")]
    [Description("Provide's a command's mention to include it in other messages.")]
    public partial IResult MentionCommand(
        [Description("The full name of the command.")]
            string commandName);

    [AutoComplete("mention-command")]
    public partial void AutoCompleteCommands(AutoComplete<string> commandName);
}