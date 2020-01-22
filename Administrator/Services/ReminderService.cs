using System;
using System.Linq;
using System.Threading.Tasks;
using Administrator.Database;
using Administrator.Extensions;
using Disqord;
using FluentScheduler;
using Microsoft.Extensions.DependencyInjection;
using TimeUnit = Humanizer.Localisation.TimeUnit;

namespace Administrator.Services
{
    public sealed class ReminderService : Service
    {
        private readonly Registry _registry;
        private readonly DiscordClient _client;
        private readonly LocalizationService _localization;

        public ReminderService(IServiceProvider provider)
            : base(provider)
        {
            _registry = _provider.GetRequiredService<Registry>();
            _client = _provider.GetRequiredService<DiscordClient>();
            _localization = _provider.GetRequiredService<LocalizationService>();
        }

        public override Task InitializeAsync()
        {
            _registry.Schedule(async () => await SendRemindersAsync())
                .NonReentrant()
                .ToRunNow()
                .AndEvery(10)
                .Seconds();

            return base.InitializeAsync();
        }

        private async Task SendRemindersAsync()
        {
            using var ctx = new AdminDatabaseContext(_provider);
            var now = DateTimeOffset.UtcNow;
            foreach (var reminder in ctx.Reminders.Where(x => now >= x.Ending))
            {
                var user = await _client.GetOrDownloadUserAsync(reminder.AuthorId);
                var language = reminder.GuildId.HasValue
                    ? (await ctx.GetOrCreateGuildAsync(reminder.GuildId.Value)).Language
                    : (await ctx.GetOrCreateGlobalUserAsync(reminder.AuthorId)).Language;

                var builder = new LocalEmbedBuilder()
                    .WithSuccessColor()
                    .WithDescription(string.Join('\n',
                        (reminder.Text ?? string.Empty).TrimTo(LocalEmbedBuilder.MAX_DESCRIPTION_LENGTH - 50),
                        Markdown.Link(_localization.Localize(language, "info_jumpmessage"),
                            $"https://discordapp.com/channels/{reminder.GuildId?.ToString() ?? "@me"}/{reminder.ChannelId}/{reminder.MessageId}")));

                try
                {
                    if (!reminder.GuildId.HasValue)
                    {
                        await user.SendMessageAsync(
                            _localization.Localize(language, "reminder_trigger",
                                (reminder.Ending - reminder.CreatedAt).HumanizeFormatted(_localization, language,
                                    TimeUnit.Second, true)), embed: builder.Build());

                        continue;
                    }

                    await _client.GetGuild(reminder.GuildId.Value).GetTextChannel(reminder.ChannelId).SendMessageAsync(
                        _localization.Localize(language, "reminder_trigger",
                            (reminder.Ending - reminder.CreatedAt).HumanizeFormatted(_localization, language,
                                TimeUnit.Second, true)), embed: builder.Build());
                }
                catch { /* ignored */ }
                finally
                {
                    ctx.Reminders.Remove(reminder);
                }
            }

            await ctx.SaveChangesAsync();
        }
    }
}