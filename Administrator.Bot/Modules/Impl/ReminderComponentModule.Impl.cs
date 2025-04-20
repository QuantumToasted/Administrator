using System.Text;
using Administrator.Bot.Jobs;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands.Components;
using Disqord.Rest;
using Microsoft.EntityFrameworkCore;
using Qmmands;
using Qommon;
using Quartz;

namespace Administrator.Bot;

public sealed partial class ReminderComponentModule(AdminDbContext db, ISchedulerFactory schedulerFactory) : DiscordComponentModuleBase
{
    public IComponentInteraction Interaction => (IComponentInteraction)Context.Interaction;
    
    public partial async Task<IResult> Snooze(int reminderId, int[] selectedValues)
    {
        var snoozeMinutes = selectedValues[0];

        if (await db.Reminders.FirstOrDefaultAsync(x => x.Id == reminderId && x.AuthorId == Context.AuthorId) is not { } reminder)
            return Response("No reminder exists with that ID.").AsEphemeral();
        
        if (snoozeMinutes == 0) // dismiss
        {
            await Interaction.Response().ModifyMessageAsync(reminder.FormatExpiryMessage<LocalInteractionMessageResponse>(false));
            return null!;
        }

        var now = Context.Interaction.CreatedAt();
        reminder.ExpiresAt = now.AddMinutes(snoozeMinutes);
        await db.SaveChangesAsync();
        
        var contentBuilder = new StringBuilder($"Reminder {reminder} has been snoozed. You will be reminded again ")
            .Append(Markdown.Timestamp(reminder.ExpiresAt, Markdown.TimestampFormat.RelativeTime))
            .AppendNewline(" about the following message:")
            .AppendNewline(reminder.Text);

        var scheduler = await schedulerFactory.GetScheduler();
        await scheduler.ScheduleAdminJob<ReminderExpiryJob, Reminder>(reminder);
        await Interaction.Response().ModifyMessageAsync(reminder.FormatExpiryMessage<LocalInteractionMessageResponse>(false));
        return Response(contentBuilder.ToString());
    }
}