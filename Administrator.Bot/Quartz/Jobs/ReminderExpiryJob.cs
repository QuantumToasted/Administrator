using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NodaTime;
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

        if (bot.TryGetAnyGuildChannel(reminder.ChannelId, out var channel))
        {
            var settings = await db.Guilds.GetValueOrDefault(channel.GuildId, g => g.Settings);
            var member = await bot.GetOrFetchMemberAsync(channel.GuildId, reminder.AuthorId);
            if (!settings.HasFlag(GuildSettings.PublicReminders) && member?.CalculateGuildPermissions().HasFlag(Permissions.ModerateMembers) != true)
            {
                Logger.LogDebug("Sending reminder #{Id} to user {UserId}'s DMs due to guild " +
                                "{GuildId} having public reminders disabled.", reminder.Id, reminder.AuthorId.RawValue, channel.GuildId.RawValue);
                await bot.TrySendDirectMessageAsync(reminder.AuthorId, expiryMessage);
                return;
            }
        }
        
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
            var now = LocalDateTime.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
            var expiresAt = LocalDateTime.FromDateTime(reminder.ExpiresAt.UtcDateTime);
            
            do
            {
                expiresAt = reminder.RepeatMode switch
                {
                    ReminderRepeatMode.Daily => expiresAt.PlusDays(reminder.RepeatInterval.Value),
                    ReminderRepeatMode.Weekly => expiresAt.PlusWeeks(reminder.RepeatInterval.Value),
                    ReminderRepeatMode.Monthly => expiresAt.PlusMonths(reminder.RepeatInterval.Value),
                    _ => throw new ArgumentOutOfRangeException()
                };

            } while (expiresAt < now);

            reminder.ExpiresAt = new ZonedDateTime(expiresAt, DateTimeZone.Utc, Offset.Zero).ToDateTimeOffset();
            await context.Scheduler.RescheduleAdminJob<ReminderExpiryJob, Reminder>(reminder);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}