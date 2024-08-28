using System.Text;
using Administrator.Core;
using Administrator.Database;
using Humanizer;
using Humanizer.Localisation;

namespace Administrator.Bot.AutoComplete;

public sealed class ReminderAutoCompleteFormatter : IAutoCompleteFormatter<Reminder, int>
{
    public string FormatAutoCompleteName(Reminder model)
    {
        var builder = new StringBuilder($"#{model.Id} - ");
        if (!model.RepeatMode.HasValue)
        {
            builder.Append($"in {(model.ExpiresAt - DateTimeOffset.UtcNow).Humanize(int.MaxValue, maxUnit: TimeUnit.Year, minUnit: TimeUnit.Second)} - ");
        }
        else
        {
            var expiresAt = model.ExpiresAt;
            builder.Append("Repeats ")
                .Append(model.RepeatMode.Value switch
                {
                    ReminderRepeatMode.Hourly => $"every {model.FormatRepeatDuration()} at {"minute".ToQuantity(expiresAt.Minute)} past the hour",
                    ReminderRepeatMode.Daily => $"daily at {expiresAt:t}",
                    ReminderRepeatMode.Weekly => $"weekly on {expiresAt.DayOfWeek}s at {expiresAt:t}",
                    _ => throw new ArgumentOutOfRangeException()
                })
                .Append(" - ");
        }

        builder.Append(model.Text);

        return builder.ToString();
    }

    public int FormatAutoCompleteValue(Reminder model)
        => model.Id;

    public Func<Reminder, string[]> ComparisonSelector => static model => [model.Text];
}