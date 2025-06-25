using Disqord.Bot.Commands.Components;
using Qmmands;

namespace Administrator.Bot;

public sealed partial class ReminderComponentModule
{
    [SelectionCommand("Reminder:Snooze:*")]
    public partial Task<IResult> Snooze(int reminderId, int[] selectedValues);
}