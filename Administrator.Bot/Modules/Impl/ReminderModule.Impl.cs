using System.Diagnostics.CodeAnalysis;
using System.Text;
using Administrator.Bot.Jobs;
using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Extensions.Interactivity.Menus.Paged;
using Disqord.Gateway;
using Disqord.Rest;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Qmmands;
using Quartz;

namespace Administrator.Bot;

public sealed partial class ReminderModule(AdminDbContext db, SlashCommandMentionService mentions, ISchedulerFactory schedulerFactory) : DiscordApplicationModuleBase
{
    public partial async Task<IResult> List()
    {
        var userReminders = await db.Reminders.Where(x => x.AuthorId == Context.AuthorId && x.ExpiresAt != x.CreatedAt)
            .OrderByDescending(x => x.ExpiresAt)
            .ToListAsync();

        if (userReminders.Count == 0)
            return Response("You don't have any active reminders!").AsEphemeral(Context.GuildId.HasValue);

        var dmChannel = await Context.Author.CreateDirectChannelAsync();
        var pages = userReminders.Chunk(10)
            .Select(x =>
            {
                return new Page()
                    .WithContent("Your reminders:")
                    .AddEmbed(new LocalEmbed()
                        .WithUnusualColor()
                        .WithFields(x.Select(y =>
                        {
                            var nameBuilder = new StringBuilder($"#{y.Id}");

                            if (y.ChannelId != dmChannel.Id && Bot.TryGetAnyGuildChannel(y.ChannelId, out var channel))
                            {
                                nameBuilder.Append($" - in #{channel.Name} ({Bot.GetGuild(channel.GuildId)!.Name})");
                            }

                            if (y.RepeatMode.HasValue)
                            {
                                nameBuilder.Append($" - repeats every {y.FormatRepeatDuration()}");
                            }

                            return new LocalEmbedField()
                                .WithName(nameBuilder.ToString())
                                .WithValue($"{Markdown.Timestamp(y.ExpiresAt, Markdown.TimestampFormat.RelativeTime)}\n{y.Text}"
                                    .Truncate(Discord.Limits.Message.Embed.Field.MaxValueLength));
                        })));
            })
            .ToList();

        return Menu(new AdminInteractionMenu(new AdminPagedView(pages, Context.GuildId.HasValue), Context.Interaction));
    }

    public partial Task<IResult> Create(DateTimeOffset expiresAt, string text)
        => Create(new ReminderCreationOptions(text, expiresAt));

    public partial Task<IResult> Repeat(string text, ReminderRepeatMode mode, int interval, DateTimeOffset? time)
        => Create(new ReminderCreationOptions(text, time, mode, interval));

    public partial async Task<IResult> Remove(int id)
    {
        if (await db.Reminders.FindAsync(id) is not { } reminder)
            return Response($"No reminder exists with the ID {id}.").AsEphemeral();

        if (reminder.AuthorId != Context.AuthorId)
            return Response($"The reminder {reminder} does not belong to you!");
        
        db.Reminders.Remove(reminder);
        await db.SaveChangesAsync();
        
        var scheduler = await schedulerFactory.GetScheduler();
        await scheduler.DeleteAdminJob<ReminderExpiryJob, Reminder>(reminder);
        
        return Response($"Your reminder {reminder} has been successfully removed.").AsEphemeral(Context.GuildId.HasValue);
    }

    public partial async Task AutoCompleteReminders(AutoComplete<int> id)
    {
        if (!id.IsFocused)
            return;

        var query = db.Reminders.OrderBy(x => x.ExpiresAt).Where(x => x.AuthorId == Context.AuthorId && x.ExpiresAt != x.CreatedAt);

        if (int.TryParse(id.RawArgument, out var rawId))
            query = query.Where(x => EF.Functions.Like(x.Id.ToString(), $"%{rawId}%"));

        var reminders = await query.ToListAsync();
        
        id.AutoComplete(Context, reminders);
    }
    
    private async Task<IResult> Create(ReminderCreationOptions options)
    {
        if (options.IsRepeating)
        {
            var now = LocalDateTime.FromDateTime(Context.Interaction.CreatedAt().UtcDateTime);
            var expiresAt = options.ExpiresAt is not null ? LocalDateTime.FromDateTime(options.ExpiresAt.Value.UtcDateTime) : now;

            while (expiresAt <= now)
            {
                expiresAt = options.RepeatMode switch
                {
                    ReminderRepeatMode.Daily => expiresAt.PlusDays(options.RepeatInterval.Value),
                    ReminderRepeatMode.Weekly => expiresAt.PlusWeeks(options.RepeatInterval.Value),
                    ReminderRepeatMode.Monthly => expiresAt.PlusMonths(options.RepeatInterval.Value),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }

            options.ExpiresAt = new ZonedDateTime(expiresAt, DateTimeZone.Utc, Offset.Zero).ToDateTimeOffset();
        }
        else if (options.ExpiresAt < Context.Interaction.CreatedAt())
        {
            return Response("You can't set a reminder for the past!\n" +
                            "(If this time isn't in the past for you, try changing your timezone with " +
                            $"{mentions.GetMention("self timezone")}.)").AsEphemeral();
        }

        var reminder = new Reminder(options.Text, Context.AuthorId, Context.ChannelId, options.ExpiresAt.Value, options.RepeatMode, options.RepeatInterval);
        db.Reminders.Add(reminder);
        await db.SaveChangesAsync();

        var scheduler = await schedulerFactory.GetScheduler();
        await scheduler.ScheduleAdminJob<ReminderExpiryJob, Reminder>(reminder);
        
        var responseBuilder = new StringBuilder($"{reminder} Reminder created. You will be reminded ");
        if (options.IsRepeating)
        {
            responseBuilder.Append("every ")
                .Append(Markdown.Bold(reminder.FormatRepeatDuration()))
                .AppendNewline(" about the following message:")
                .AppendNewline(options.Text)
                .Append("(Next time you'll be reminded: ")
                .Append(Markdown.Timestamp(reminder.ExpiresAt, Markdown.TimestampFormat.RelativeTime))
                .Append(')');
        }
        else
        {
            responseBuilder.Append(Markdown.Timestamp(reminder.ExpiresAt, Markdown.TimestampFormat.RelativeTime))
                .AppendNewline(" about the following message:")
                .AppendNewline(options.Text);
        }

        var user = await db.Users.GetOrCreateAsync(Context.AuthorId);
        if (user.TimeZone is null)
        {
            responseBuilder.AppendNewline()
                .AppendNewline()
                .Append($"(Time/date looks weird? Use the {mentions.GetMention("self timezone")} command to set your timezone.)");
        }

        return Response(responseBuilder.ToString());
    }

    private record ReminderCreationOptions(string Text, 
        DateTimeOffset? ExpiresAt, 
        ReminderRepeatMode? RepeatMode = null,
        int? RepeatInterval = null)
    {
        public DateTimeOffset? ExpiresAt { get; set; } = ExpiresAt;
        
        [MemberNotNullWhen(true, nameof(RepeatMode), nameof(RepeatInterval))]
        [MemberNotNullWhen(false, nameof(ExpiresAt))]
        public bool IsRepeating => RepeatMode.HasValue;
    }
}