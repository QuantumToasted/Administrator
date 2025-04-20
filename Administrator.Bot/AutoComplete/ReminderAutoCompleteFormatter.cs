using System.Text;
using Administrator.Core;
using Administrator.Database;
using Humanizer;
using Humanizer.Localisation;
using Qmmands;

namespace Administrator.Bot.AutoComplete;

public sealed class ReminderAutoCompleteFormatter : IAutoCompleteFormatter<Reminder, int>
{
    public static string FormatAutoCompleteName(ICommandContext context, Reminder model)
    {
        var builder = new StringBuilder($"#{model.Id} - ");
        if (!model.RepeatMode.HasValue)
        {
            builder.Append($"in {(model.ExpiresAt - DateTimeOffset.UtcNow).Humanize(int.MaxValue, maxUnit: TimeUnit.Year, minUnit: TimeUnit.Second)} - ");
        }
        else
        {
            builder.Append($"Repeats every {model.FormatRepeatDuration()} - ");
        }

        builder.Append(model.Text);

        return builder.ToString();
    }

    public static int FormatAutoCompleteValue(ICommandContext context, Reminder model) => model.Id;
    public static string[] FormatComparisonValues(ICommandContext context, Reminder model) => [model.Text];
}