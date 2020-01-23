using System;
using System.Linq;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Database;
using Administrator.Extensions;
using Administrator.Services;
using Disqord;
using Humanizer.Localisation;
using Microsoft.EntityFrameworkCore;
using Qmmands;

namespace Administrator.Commands.Reminders
{
    [Name("Reminders")]
    [Group("reminder", "remind")]
    public sealed class ReminderCommands : AdminModuleBase
    {
        public PaginationService Pagination { get; set; }

        [Command("", "create"), RunMode(RunMode.Parallel)]
        public async ValueTask<AdminCommandResult> CreateReminderAsync(TimeSpan duration,
            [Remainder] string text = null)
        {
            if (duration > TimeSpan.FromDays(365 * 10)) // 10 years, jesus christ
                return CommandErrorLocalized("reminder_toolong");

            var reminder = Context.Database.Reminders.Add(new Reminder(Context.User.Id, Context.Guild?.Id,
                Context.Channel.Id, Context.Message.Id, text, duration)).Entity;
            await Context.Database.SaveChangesAsync();

            return CommandSuccessLocalized("reminder_create", args: Markdown.Code($"[#{reminder.Id}]"));
        }

        [Command("list")]
        public async ValueTask<AdminCommandResult> ListRemindersAsync()
        {
            var reminders = await Context.Database.Reminders.Where(x => x.AuthorId == Context.User.Id)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            if (!Context.IsPrivate)
                reminders = reminders.Where(x => x.GuildId == Context.Guild.Id).ToList();

            if (reminders.Count == 0)
                return CommandErrorLocalized("reminder_list_none");

            var pages = DefaultPaginator.GeneratePages(reminders, 10, reminder => new LocalEmbedFieldBuilder()
                    .WithName(Localize("", reminder.Id,
                        (reminder.Ending - DateTimeOffset.UtcNow).HumanizeFormatted(Localization, Context.Language,
                            TimeUnit.Second)))
                    .WithValue(reminder.Text ??
                               Markdown.Link(Localize("info_jumpmessage"),
                                   $"https://discordapp.com/channels/{reminder.GuildId?.ToString() ?? "@me"}/{reminder.ChannelId}/{reminder.MessageId}")),
                builderFunc: () => new LocalEmbedBuilder()
                    .WithTitle(Localize("reminder_list_title"))
                    .WithSuccessColor());

            if (pages.Count > 1)
            {
                await Pagination.SendPaginatorAsync(Context.Channel, new DefaultPaginator(pages, 0), pages[0]);
                return CommandSuccess();
            }

            return CommandSuccess(embed: pages[0].Embed);
        }

        [Command("delete")]
        public async ValueTask<AdminCommandResult> DeleteReminderAsync([MustBe(Operator.GreaterThan, 0)] int id)
        {
            if (!(await Context.Database.Reminders.FindAsync(id) is { } reminder) ||
                reminder.AuthorId != Context.User.Id)
                return CommandErrorLocalized("reminder_delete_notfound");

            Context.Database.Reminders.Remove(reminder);
            await Context.Database.SaveChangesAsync();

            return CommandSuccessLocalized("reminder_delete");
        }
    }
}