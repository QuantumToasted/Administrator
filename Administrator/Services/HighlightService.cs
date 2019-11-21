using Administrator.Database;
using System;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Administrator.Extensions;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Disqord;
using Disqord.Events;

namespace Administrator.Services
{
    public sealed class HighlightService : IService, IHandler<MessageReceivedEventArgs>
    {
        private readonly LoggingService _logging;
        private readonly LocalizationService _localization;
        private readonly DiscordClient _client;
        private readonly IServiceProvider _provider;

        public HighlightService(LoggingService logging, LocalizationService localization, 
            DiscordClient client, IServiceProvider provider)
        {
            _logging = logging;
            _localization = localization;
            _client = client;
            _provider = provider;
        }

        public async Task HandleAsync(MessageReceivedEventArgs args)
        {
            if (!(args.Message is CachedUserMessage message))
                return;

            using var ctx = new AdminDatabaseContext(_provider);
            if (string.IsNullOrWhiteSpace(message.Content) || message.Author.IsBot ||
                !(message.Channel is CachedTextChannel channel)) return;

            var highlights = await ctx.Highlights.ToListAsync(); // TODO: Not separate into two queries. Blame EF Core.

            var completedHighlights = new List<ulong>();
            foreach (var highlight in highlights.Where(x => x.UserId != message.Author.Id &&
                    Regex.IsMatch(message.Content, $@"\b{x.Text}\b", RegexOptions.IgnoreCase)))
            {
                if (completedHighlights.Contains(highlight.UserId) ||
                    highlight.GuildId.HasValue && highlight.GuildId != channel.Guild.Id) continue;

                if (channel.Guild.GetMember(highlight.UserId) is { } member &&
                    member.GetPermissionsFor(channel).ViewChannel &&
                    channel.GetMessages().OrderByDescending(x => x.Id)
                        .Where(x => DateTimeOffset.UtcNow - x.Id.CreatedAt < TimeSpan.FromMinutes(15))
                        .Take(50).All(x => x.Author.Id != highlight.UserId))
                {
                    var user = await ctx.GetOrCreateGlobalUserAsync(highlight.UserId);
                    if (user.HighlightBlacklist.Contains(message.Author.Id) ||
                        user.HighlightBlacklist.Contains(message.Channel.Id)) continue;

                    var target = await _client.GetUserAsync(highlight.UserId);
                    var builder = new LocalEmbedBuilder()
                        .WithSuccessColor()
                        .WithAuthor(_localization.Localize(user.Language, "highlight_trigger_author",
                            message.Author.ToString(), $"#{message.Channel}"), message.Author.GetAvatarUrl())
                        .WithDescription(new StringBuilder()
                            .AppendLine(message.Content.TrimTo(LocalEmbedBuilder.MAX_DESCRIPTION_LENGTH - 50))
                            .AppendLine()
                            .AppendLine($"[{_localization.Localize(user.Language, "info_jumpmessage")}]({message.JumpUrl})")
                            .ToString())
                        .WithTimestamp(message.Id.CreatedAt);

                    _ = target.SendMessageAsync(_localization.Localize(user.Language, "highlight_trigger_text",
                        channel.Guild.Name.Sanitize()), embed: builder.Build());
                }
            }
        }

        public Task InitializeAsync()
            => _logging.LogInfoAsync("Initialized", "Highlights");
    }
}
