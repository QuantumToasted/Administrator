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
    public static LocalMessage FormatExpiryMessage(this Reminder reminder)
    {
        var contentBuilder = new StringBuilder(Mention.User(reminder.AuthorId));
        contentBuilder.AppendNewline(reminder.RepeatMode.HasValue
            ? $", your reminder {reminder} for every {Markdown.Code(reminder.FormatRepeatDuration())}:"
            : $", your reminder {reminder} from {Markdown.Timestamp(reminder.CreatedAt, Markdown.TimestampFormat.RelativeTime)}:");
        
        contentBuilder.Append(reminder.Text);
        return new LocalMessage()
            .WithContent(contentBuilder.ToString())
            .WithAllowedMentions(new LocalAllowedMentions().WithUserIds(reminder.AuthorId));
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

    
    public static async Task RemindAsync(this Reminder reminder, DiscordBotBase bot)
    {
        await using var scope = bot.Services.CreateAsyncScopeWithDatabase(out var db);

        if (!reminder.RepeatMode.HasValue)
        {
            try
            {
                await bot.StartMenuAsync(reminder.ChannelId, new AdminTextMenu(new ReminderSnoozeView(reminder)), TimeSpan.FromHours(2));
            }
            catch { /*ignored */ }

            db.Reminders.Remove(reminder);
        }
        else
        {
            await bot.TrySendMessageAsync(reminder.ChannelId, reminder.FormatExpiryMessage());

            var existingReminder = await db.Reminders.FindAsync(reminder.Id);
            var now = DateTimeOffset.UtcNow;
            
            do
            {
                existingReminder!.ExpiresAt = existingReminder.RepeatMode!.Value switch
                {
                    ReminderRepeatMode.Hourly => existingReminder.ExpiresAt.AddHours(reminder.RepeatInterval!.Value),
                    ReminderRepeatMode.Daily => existingReminder.ExpiresAt.AddDays(reminder.RepeatInterval!.Value),
                    ReminderRepeatMode.Weekly => existingReminder.ExpiresAt.AddWeeks(reminder.RepeatInterval!.Value),
                    _ => throw new ArgumentOutOfRangeException()
                };

            } while (existingReminder.ExpiresAt < now);
        }
        
        await db.SaveChangesAsync();
    }
}