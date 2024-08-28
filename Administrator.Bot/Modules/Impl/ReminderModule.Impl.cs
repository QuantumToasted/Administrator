using System.Text;
using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Extensions.Interactivity.Menus.Paged;
using Disqord.Gateway;
using Disqord.Rest;
using Microsoft.EntityFrameworkCore;
using Qmmands;

namespace Administrator.Bot;

public sealed partial class ReminderModule(ReminderService reminders, AdminDbContext db, SlashCommandMentionService mentions) : DiscordApplicationModuleBase
{
    public partial async Task<IResult> List()
    {
        var userReminders = await db.Reminders.Where(x => x.AuthorId == Context.AuthorId)
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
    
    public partial async Task<IResult> Create(string text, DateTimeOffset expiresAt)
    {
        var result = await reminders.CreateReminderAsync(text, expiresAt);
        if (!result.IsSuccessful)
            return Response(result.ErrorMessage).AsEphemeral();

        var globalUser = await db.Users.GetOrCreateAsync(Context.AuthorId);
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

    public partial async Task<IResult> Repeat(string text, ReminderRepeatMode mode, double interval, DateTimeOffset? time)
    {
        var result = await reminders.CreateReminderAsync(text, mode, interval, time);
        if (!result.IsSuccessful)
            return Response(result.ErrorMessage).AsEphemeral();

        var globalUser = await db.Users.GetOrCreateAsync(Context.AuthorId);
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

    public partial async Task<IResult> Remove(int id)
    {
        var result = await reminders.RemoveReminderAsync(id);
        if (!result.IsSuccessful)
            return Response(result.ErrorMessage).AsEphemeral();
        
        return Response($"Your reminder {result.Value} has been successfully removed.").AsEphemeral(Context.GuildId.HasValue);
    }

    public partial Task AutoCompleteReminders(AutoComplete<int> id)
        => id.IsFocused ? reminders.AutoCompleteRemindersAsync(id) : Task.CompletedTask;
}