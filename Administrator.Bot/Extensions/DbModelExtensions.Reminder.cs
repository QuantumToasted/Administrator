using System.Text;
using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Bot;
using Disqord.Extensions.Interactivity.Menus;
using Humanizer;

namespace Administrator.Bot;

public static partial class DbModelExtensions
{
    public static TMessage FormatExpiryMessage<TMessage>(this Reminder reminder, bool includeComponents = true)
        where TMessage : LocalMessageBase, new()
    {
        var contentBuilder = new StringBuilder(Mention.User(reminder.AuthorId));
        contentBuilder.AppendNewline(reminder.RepeatMode.HasValue
            ? $", your reminder {reminder} for every {Markdown.Code(reminder.FormatRepeatDuration())}:"
            : $", your reminder {reminder} from {Markdown.Timestamp(reminder.CreatedAt, Markdown.TimestampFormat.RelativeTime)}:");
        
        contentBuilder.Append(reminder.Text);

        var message = new TMessage()
            .WithContent(contentBuilder.ToString())
            .WithAllowedMentions(new LocalAllowedMentions().WithUserIds(reminder.AuthorId));
        
        // single reminder - allowed snoozing
        if (!reminder.RepeatMode.HasValue && includeComponents)
        {
            var selection = LocalComponent.Selection($"Reminder:Snooze:{reminder.Id}",
                new LocalSelectionComponentOption("5 minutes", "5"),
                new LocalSelectionComponentOption("10 minutes", "10"),
                new LocalSelectionComponentOption("30 minutes", "30"),
                new LocalSelectionComponentOption("1 hour", "60"),
                new LocalSelectionComponentOption("8 hours", "480"),
                new LocalSelectionComponentOption("12 hours", "720"),
                new LocalSelectionComponentOption("24 hours", "1440"),
                new LocalSelectionComponentOption("Dismiss", "0"));

            message.AddComponent(LocalComponent.Row(selection));
        }
        else
        {
            message.WithComponents();
        }

        return message;
    }
    
    public static string FormatRepeatDuration(this Reminder reminder)
    {
        if (!reminder.RepeatMode.HasValue)
            throw new InvalidOperationException("Only repeating reminders can be formatted in this way.");

        var value = reminder.RepeatMode.Value switch
        {
            ReminderRepeatMode.Hourly => TimeSpan.FromHours(reminder.RepeatInterval!.Value),
            ReminderRepeatMode.Daily => TimeSpan.FromDays(reminder.RepeatInterval!.Value),
            ReminderRepeatMode.Weekly => TimeSpan.FromDays(reminder.RepeatInterval!.Value * 7),
            _ => throw new ArgumentOutOfRangeException()
        };

        return value.Humanize();
        //return value.Humanize(int.MaxValue, maxUnit: TimeUnit.Week, minUnit: TimeUnit.Minute);
    }
}