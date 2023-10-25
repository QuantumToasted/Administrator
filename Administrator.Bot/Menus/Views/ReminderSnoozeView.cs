using System.Text;
using Administrator.Database;
using Disqord;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Rest;

namespace Administrator.Bot;

public sealed class ReminderSnoozeView : AdminViewBase
{
    private readonly Reminder _reminder;

    public ReminderSnoozeView(Reminder reminder)
        : base(null)
    {
        _reminder = reminder;
        
        MessageTemplate = x =>
        {
            var message = reminder.FormatExpiryMessage();
            x.Content = message.Content;
            x.AllowedMentions = message.AllowedMentions;
        };
    }

    [Selection(Placeholder = "Snooze reminder...")]
    [SelectionOption("10 minutes", Value = "10")]
    [SelectionOption("30 minutes", Value = "30")]
    [SelectionOption("1 hour", Value = "60")]
    [SelectionOption("8 hours", Value = "480")]
    [SelectionOption("12 hours", Value = "720")]
    [SelectionOption("1 day", Value = "1440")]
    public async ValueTask SnoozeAsync(SelectionEventArgs e)
    {
        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        
        var snoozeMinutes = int.Parse(e.SelectedOptions[0].Value.Value);
        
        var now = e.Interaction.CreatedAt();

        _reminder.ExpiresAt = now.AddMinutes(snoozeMinutes);

        db.Reminders.Add(_reminder);
        await db.SaveChangesAsync();
        
        var contentBuilder = new StringBuilder($"Reminder {_reminder} has been snoozed. You will be reminded again ")
            .Append(Markdown.Timestamp(_reminder.ExpiresAt, Markdown.TimestampFormat.RelativeTime))
            .AppendNewline(" about the following message:")
            .AppendNewline(_reminder.Text);
            
        //MessageTemplate = x => x.WithContent(responseBuilder.ToString());

        await e.Interaction.Response().SendMessageAsync(new LocalInteractionMessageResponse()
            .WithContent(contentBuilder.ToString())
            .WithIsEphemeral());
        
        ClearComponents();
    }
}