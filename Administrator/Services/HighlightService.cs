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
using Microsoft.Extensions.DependencyInjection;

namespace Administrator.Services
{
    public sealed class HighlightService : Service, IHandler<MessageReceivedEventArgs>
    {
        private readonly LocalizationService _localization;
        private readonly DiscordClient _client;

        public HighlightService(IServiceProvider provider)
            : base(provider)
        {
            _localization = _provider.GetRequiredService<LocalizationService>();
            _client = _provider.GetRequiredService<DiscordClient>();
        }

        public async Task HandleAsync(MessageReceivedEventArgs args)
        {
            if (!(args.Message is CachedUserMessage message))
                return;

            using var ctx = new AdminDatabaseContext(_provider);
            if (string.IsNullOrWhiteSpace(message.Content) || message.Author.IsBot ||
                !(message.Channel is CachedTextChannel channel)) return;

            var completedHighlights = new List<ulong>();
            foreach (var highlight in ctx.Highlights.Where(x => x.UserId != message.Author.Id &&
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
                            message.Author.Tag, message.Channel), message.Author.GetAvatarUrl())
                        .WithDescription(new StringBuilder()
                            .AppendNewline(message.Content.TrimTo(LocalEmbedBuilder.MAX_DESCRIPTION_LENGTH - 50))
                            .AppendNewline()
                            .AppendNewline($"[{_localization.Localize(user.Language, "info_jumpmessage")}]({message.JumpUrl})")
                            .ToString())
                        .WithTimestamp(message.Id.CreatedAt);

                    _ = target.SendMessageAsync(_localization.Localize(user.Language, "highlight_trigger_text",
                        channel.Guild.Name.Sanitize()), embed: builder.Build());

                    completedHighlights.Add(highlight.UserId);
                }
            }
        }
    }
}
