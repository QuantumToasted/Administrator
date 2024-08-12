using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Disqord.Bot.Commands.Interaction;
using Microsoft.EntityFrameworkCore;

namespace Administrator.Bot;

// TODO: maybe remove this or extrapolate into a util method. I don't like the idea of coupling modules to services.
[ScopedService]
public sealed class ReminderService(AdminDbContext db, ICommandContextAccessor contextAccessor, SlashCommandMentionService mentions, 
    ReminderExpiryService expiryService, AutoCompleteService autoComplete)
{
    private readonly IDiscordInteractionCommandContext _context = (IDiscordInteractionCommandContext)contextAccessor.Context;

    public async Task<Result<Reminder>> CreateReminderAsync(string text, DateTimeOffset expiresAt)
    {
        expiresAt = expiresAt.ToUniversalTime();
        
        var now = _context.Interaction.CreatedAt();

        if (expiresAt < now)
        {
            return "You can't set a reminder for the past!\n" +
                   "(If this time isn't in the past for you, try changing your timezone with " +
                   $"{mentions.GetMention("self timezone")}.)";
        }

        var reminder = new Reminder(text, _context.AuthorId, _context.ChannelId, expiresAt, null, null);
        
        db.Reminders.Add(reminder);
        await db.SaveChangesAsync();
        expiryService.CancelCts();

        return reminder;
    }
    
    public async Task<Result<Reminder>> CreateReminderAsync(string text, ReminderRepeatMode repeatMode, double repeatInterval, DateTimeOffset? initialReminder)
    {
        DateTimeOffset expiresAt;
        if (!initialReminder.HasValue)
        {
            var now = _context.Interaction.CreatedAt();

            expiresAt = now;
            while (expiresAt <= now)
            {
                expiresAt = repeatMode switch
                {
                    ReminderRepeatMode.Hourly => expiresAt.AddHours(repeatInterval),
                    ReminderRepeatMode.Daily => expiresAt.AddDays(repeatInterval),
                    ReminderRepeatMode.Weekly => expiresAt.AddWeeks(repeatInterval),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
        }
        else
        {
            expiresAt = initialReminder.Value;
        }

        var reminder = new Reminder(text, _context.AuthorId, _context.ChannelId, expiresAt, repeatMode, repeatInterval);
        
        db.Reminders.Add(reminder);
        await db.SaveChangesAsync();
        expiryService.CancelCts();

        return reminder;
    }

    public async Task<Result<Reminder>> RemoveReminderAsync(int id)
    {
        if (await db.Reminders.FindAsync(id) is not { } reminder)
            return $"No reminder exists with the ID {id}.";

        if (reminder.AuthorId != _context.AuthorId)
            return $"The reminder {reminder} does not belong to you!";

        db.Reminders.Remove(reminder);
        await db.SaveChangesAsync();
        expiryService.CancelCts();

        return reminder;
    }

    public async Task AutoCompleteRemindersAsync(AutoComplete<int> id)
    {
        if (!id.IsFocused)
            return;

        var reminders = await db.Reminders.Where(x => x.AuthorId == _context.AuthorId)
            .OrderBy(x => x.ExpiresAt)
            .ToListAsync();
        
        autoComplete.AutoComplete(id, reminders);
    }
}