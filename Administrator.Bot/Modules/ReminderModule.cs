using System.Text;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands.Application;
using Qmmands;

namespace Administrator.Bot;

[SlashGroup("reminder")]
public sealed class ReminderModule(ReminderService reminders, AdminDbContext db, SlashCommandMentionService mentions) : DiscordApplicationModuleBase
{
    [SlashCommand("create")]
    [Description("Creates a new non-repeating reminder.")]
    public async Task<IResult> CreateAsync(
        [Description("The text to be reminded about.")]
            string text,
        [Name("time")]
        [Description("A duration (2h30m) or instant in time (tomorrow at noon).")]
            DateTimeOffset expiresAt)
    {
        var result = await reminders.CreateReminderAsync(text, expiresAt);
        if (!result.IsSuccessful)
            return Response(result.ErrorMessage).AsEphemeral();

        var globalUser = await db.GetOrCreateGlobalUserAsync(Context.AuthorId);
        var reminder = result.Value;
        
        var responseBuilder = new StringBuilder($"{reminder} Reminder created. You will be reminded ")
            .Append(Markdown.Timestamp(reminder.ExpiresAt, Markdown.TimestampFormat.RelativeTime))
            .AppendNewline(" about the following message:")
            .AppendNewline(text);
        
        if (globalUser.TimeZone is null)
        {
            responseBuilder.AppendNewline()
                .Append($"(Time looks weird? Use the {mentions.GetMention("self timezone")} command to set your timezone.)");
        }
            
        return Response(responseBuilder.ToString());
    }

    [SlashCommand("repeat")]
    [Description("Creates a new repeating reminder.")]
    public async Task<IResult> CreateRepeatingAsync(
        [Description("The text to be reminded about.")]
            string text,
        [Description("The repeat mode for this reminder.")]
            ReminderRepeatMode mode,
        [Description("The interval (default: 1) that this reminder will repeat.")]
        [Minimum(1)]
            double interval = 1,
        [Description("A duration (2h30m) or instant in time (tomorrow at noon). Defaults to now.")]
            DateTimeOffset? time = null)
    {
        var result = await reminders.CreateReminderAsync(text, mode, interval);
        if (!result.IsSuccessful)
            return Response(result.ErrorMessage).AsEphemeral();

        var globalUser = await db.GetOrCreateGlobalUserAsync(Context.AuthorId);
        var reminder = result.Value;

        var responseBuilder = new StringBuilder($"{reminder} Reminder created. You will be reminded every ")
            .Append(Markdown.Code(reminder.FormatRepeatDuration()))
            .AppendNewline(" about the following message:")
            .AppendNewline(text)
            .Append("(Next time you'll be reminded: ")
            .Append(Markdown.Timestamp(reminder.ExpiresAt, Markdown.TimestampFormat.RelativeTime))
            .Append(')');
        
        if (globalUser.TimeZone is null)
        {
            responseBuilder.AppendNewline()
                .AppendNewline()
                .Append($"(Time/date looks weird? Use the {mentions.GetMention("self timezone")} command to set your timezone.)");
        }

        return Response(responseBuilder.ToString());
    }

    [SlashCommand("remove")]
    [Description("Removes one of your existing reminders.")]
    public async Task<IResult> RemoveAsync(
        [Description("The ID of the reminder to remove.")]
        [Minimum(1)]
            int id)
    {
        var result = await reminders.RemoveReminderAsync(id);
        if (!result.IsSuccessful)
            return Response(result.ErrorMessage).AsEphemeral();

        return Response($"Your reminder {result.Value} has been successfully removed.").AsEphemeral();
    }

    [AutoComplete("remove")]
    public Task AutoCompleteReminderAsync(AutoComplete<int> id)
        => id.IsFocused ? reminders.AutoCompleteRemindersAsync(id) : Task.CompletedTask;
}