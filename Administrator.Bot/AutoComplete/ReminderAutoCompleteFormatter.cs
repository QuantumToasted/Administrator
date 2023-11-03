using System.Text;
using Administrator.Core;
using Administrator.Database;
using Disqord;
using Humanizer;
using Humanizer.Localisation;

namespace Administrator.Bot.AutoComplete;

public sealed class ReminderAutoCompleteFormatter : IAutoCompleteFormatter<Reminder, int>
{
    public string[] FormatAutoCompleteNames(IClient client, Reminder model)
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

        return new[] { builder.ToString() };
    }

    public int FormatAutoCompleteValue(IClient client, Reminder model)
        => model.Id;

    public Func<Reminder, string[]> ComparisonSelector => static reminder => new[] { reminder.Id.ToString() };
}