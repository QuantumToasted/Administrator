using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Bot;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Administrator.Bot.Jobs;

public sealed class ReminderExpiryJob(ILogger<ReminderExpiryJob> logger, DiscordBotBase bot, AdminDbContext db) : IAdminJob<ReminderExpiryJob, Reminder>
{
    public ILogger Logger { get; } = logger;

    public async ValueTask<Reminder> GetEntity(IJobExecutionContext context, int reminderId)
    {
        var reminder = await db.Reminders.FirstAsync(x => x.Id == reminderId);
        return reminder;
    }

    public async ValueTask Execute(IJobExecutionContext context, Reminder reminder)
    {
        var expiryMessage = reminder.FormatExpiryMessage<LocalMessage>();
        
        Logger.LogDebug("Sending reminder #{Id} to channel {ChannelId}.", reminder.Id, reminder.ChannelId.RawValue);
        if (await bot.TrySendMessageAsync(reminder.ChannelId, expiryMessage) is null) // failed to send - try DMing
        {
            Logger.LogDebug("Failed to send reminder #{Id} to channel {ChannelId}. Trying user {UserId}'s DMs instead.", 
                reminder.Id, reminder.ChannelId.RawValue, reminder.AuthorId.RawValue);
            
            await bot.TrySendDirectMessageAsync(reminder.AuthorId, expiryMessage);
        }
    }

    public async ValueTask Reschedule(IJobExecutionContext context, Reminder reminder, CancellationToken cancellationToken)
    {
        if (!reminder.RepeatInterval.HasValue)
        {
            // TODO: document somewhere other than here that we are setting CreatedAt = ExpiresAt to indicate an "expired" one-off reminder.
            reminder.ExpiresAt = reminder.CreatedAt;
        }
        else
        {
            var now = DateTimeOffset.UtcNow;
            
            do
            {
                reminder.ExpiresAt = reminder.RepeatMode!.Value switch
                {
                    ReminderRepeatMode.Hourly => reminder.ExpiresAt.AddHours(reminder.RepeatInterval!.Value),
                    ReminderRepeatMode.Daily => reminder.ExpiresAt.AddDays(reminder.RepeatInterval!.Value),
                    ReminderRepeatMode.Weekly => reminder.ExpiresAt.AddWeeks(reminder.RepeatInterval!.Value),
                    _ => throw new ArgumentOutOfRangeException()
                };

            } while (reminder.ExpiresAt < now);

            await context.Scheduler.RescheduleAdminJob<ReminderExpiryJob, Reminder>(context.Trigger.Key, reminder, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}