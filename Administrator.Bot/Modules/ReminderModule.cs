using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands.Application;
using Qmmands;

namespace Administrator.Bot;

[SlashGroup("reminder")]
public sealed partial class ReminderModule
{
    [SlashCommand("list")]
    [Description("Lists all your reminders.")]
    public partial Task<IResult> List();

    [SlashCommand("create")]
    [Description("Creates a new non-repeating reminder.")]
    public partial Task<IResult> Create(
        [Name("time")]
        [Description("A duration (2h30m) or instant in time (tomorrow at noon).")]
            DateTimeOffset expiresAt,
        [Description("The text to be reminded about.")]
        [Maximum(Discord.Limits.Message.Embed.Field.MaxValueLength)]
            string text);

    [SlashCommand("repeat")]
    [Description("Creates a new repeating reminder.")]
    public partial Task<IResult> Repeat(
        [Description("The text to be reminded about.")]
            string text,
        [Description("The repeat mode for this reminder.")]
            ReminderRepeatMode mode,
        [Description("The interval (default: 1) that this reminder will repeat.")]
        [Minimum(1)]
            int interval = 1,
        [Description("A duration (2h30m) or instant in time (tomorrow at noon) to start. Defaults to now.")]
            DateTimeOffset? time = null);

    [SlashCommand("remove")]
    [Description("Removes one of your existing reminders.")]
    public partial Task<IResult> Remove(
        [Description("The ID of the reminder to remove.")]
        [Minimum(1)]
            int id);

    [AutoComplete("remove")]
    public partial Task AutoCompleteReminders(AutoComplete<int> id);
}