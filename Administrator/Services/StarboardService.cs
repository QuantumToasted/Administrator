using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Database;
using Administrator.Extensions;
using Disqord;
using Disqord.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Administrator.Services
{
    public sealed class StarboardService : Service,
        IHandler<ReactionAddedEventArgs>,
        IHandler<ReactionRemovedEventArgs>,
        IHandler<ReactionsClearedEventArgs>,
        IHandler<MessageDeletedEventArgs>,
        IHandler<MessagesBulkDeletedEventArgs>
    {
        private readonly LocalizationService _localization;

        public StarboardService(IServiceProvider provider)
            : base(provider)
        {
            _localization = _provider.GetRequiredService<LocalizationService>();
        }

        public async Task HandleAsync(ReactionAddedEventArgs args)
        {
            if (!(args.Channel is CachedTextChannel channel))
                return;

            if (args.User.HasValue && args.User.Value.IsBot)
                return;

            using var ctx = new AdminDatabaseContext(_provider);
            var star = await ctx.GetSpecialEmojiAsync(channel.Guild.Id, EmojiType.Star);
            if (!args.Emoji.Equals(star)) return;

            if (!(await ctx.GetLoggingChannelAsync(channel.Guild.Id, LogType.Starboard) is { } starboardChannel))
                return;

            var guild = await ctx.GetOrCreateGuildAsync(channel.Guild.Id);
            if (guild.BlacklistedStarboardIds.ContainsAny(channel.Id.RawValue, args.User.Id.RawValue))
                return;

            var message = args.Message.HasValue
                ? args.Message.Value as IUserMessage
                : await args.Message.Downloadable.DownloadAsync() as IUserMessage;

            if (message is null)
                return;

            if (!(await ctx.Starboard.FirstOrDefaultAsync(x => x.MessageId == message.Id
                                                               || x.EntryMessageId == message.Id) is { } entry))
            {
                var stars = (await message.GetReactionsAsync(star)).Select(x => x.Id.RawValue).ToList();
                stars = stars.Where(x => !guild.BlacklistedStarboardIds.Contains(x)).ToList();

                if (stars.Count >= guild.MinimumStars)
                {
                    var entryMessage = await starboardChannel.SendMessageAsync(
                        $"{star} {stars.Count} `#{channel.Name}` {_localization.Localize(guild.Language, "info_id")}: `{message.Id}`",
                        embed: new LocalEmbedBuilder()
                            .WithSuccessColor()
                            .WithAuthor(message.Author)
                            .WithDescription(new StringBuilder()
                                .AppendNewline((message.Content ?? message.Embeds.FirstOrDefault()?.Description)?
                                    .TrimTo(LocalEmbedBuilder.MAX_DESCRIPTION_LENGTH - 50))
                                .AppendNewline()
                                .AppendNewline(Markdown.Link(_localization.Localize(guild.Language, "info_jumpmessage"),
                                    $"https://discordapp.com/channels/{channel.Guild.Id}/{channel.Id}/{message.Id}"))
                                .ToString())
                            .WithImageUrl(
                                message.Attachments.FirstOrDefault(x => x.FileName.HasImageExtension(out _))?.Url ??
                                message.Embeds.FirstOrDefault()?.Image?.Url)
                            .WithTimestamp(message.Id.CreatedAt)
                            .Build());

                    ctx.Starboard.Add(new StarboardEntry(message.Id, message.ChannelId, channel.Guild.Id,
                        message.Author.Id, stars, entryMessage.Id, entryMessage.ChannelId));
                    await ctx.SaveChangesAsync();
                }

                return;
            }

            if (entry.Stars.Contains(args.User.Id))
                return;

            entry.Stars.Add(args.User.Id);
            ctx.Starboard.Update(entry);
            await ctx.SaveChangesAsync();

            await starboardChannel.ModifyMessageAsync(entry.EntryMessageId,
                x => x.Content = $"{star} {entry.Stars.Count} `#{channel.Name}` {_localization.Localize(guild.Language, "info_id")}: `{message.Id}`");
        }

        public async Task HandleAsync(ReactionRemovedEventArgs args)
        {
            if (!(args.Channel is CachedTextChannel channel))
                return;

            if (args.User.HasValue && args.User.Value.IsBot)
                return;

            using var ctx = new AdminDatabaseContext(_provider);
            var star = await ctx.GetSpecialEmojiAsync(channel.Guild.Id, EmojiType.Star);

            if (!args.Emoji.Equals(star)) return;

            if (!(await ctx.GetLoggingChannelAsync(channel.Guild.Id, LogType.Starboard) is { } starboardChannel))
                return;

            var guild = await ctx.GetOrCreateGuildAsync(channel.Guild.Id);
            if (guild.BlacklistedStarboardIds.ContainsAny(channel.Id.RawValue, args.User.Id.RawValue))
                return;

            var message = args.Message.HasValue
                ? args.Message.Value as IUserMessage
                : await args.Message.Downloadable.DownloadAsync() as IUserMessage;

            if (message is null)
                return;

            if (!(await ctx.Starboard.FirstOrDefaultAsync(x => x.MessageId == args.Message.Id
                                                               || x.EntryMessageId == args.Message.Id) is { } entry))
                return;

            if (!entry.Stars.Remove(args.User.Id))
                return;
            
            if (entry.Stars.Count == 0)
            {
                ctx.Starboard.Remove(entry);
                await ctx.SaveChangesAsync();
                await args.Client.DeleteMessageAsync(entry.EntryChannelId, entry.EntryMessageId);
                return;
            }

            ctx.Starboard.Update(entry);
            await ctx.SaveChangesAsync();

            await starboardChannel.ModifyMessageAsync(entry.EntryMessageId,
                x => x.Content = $"{star} {entry.Stars.Count} `#{channel.Name}` {_localization.Localize(guild.Language, "info_id")}: `{message.Id}`");
        }

        public async Task HandleAsync(ReactionsClearedEventArgs args)
        {
            if (!(args.Channel is CachedTextChannel channel))
                return;

            using var ctx = new AdminDatabaseContext(_provider);
            var star = await ctx.GetSpecialEmojiAsync(channel.Guild.Id, EmojiType.Star);

            if (!args.Reactions.HasValue || !args.Reactions.Value.ContainsKey(star)) // TODO: ContainsKey works for Equals?
                return;

            if (!(await ctx.Starboard.FindAsync(args.Message.Id) is { } entry))
                return;

            ctx.Starboard.Remove(entry);
            await ctx.SaveChangesAsync();

            await args.Client.DeleteMessageAsync(entry.EntryChannelId, entry.EntryMessageId);
        }

        public async Task HandleAsync(MessageDeletedEventArgs args)
        {
            if (!(args.Channel is CachedTextChannel channel))
                return;

            using var ctx = new AdminDatabaseContext(_provider);
            if (!(await ctx.Starboard.FirstOrDefaultAsync(x => x.MessageId == args.Message.Id
                                                               || x.EntryMessageId == args.Message.Id) is { } entry))
                return;

            ctx.Starboard.Remove(entry);
            await ctx.SaveChangesAsync();

            if (args.Message.Id != entry.EntryMessageId)
                await args.Client.DeleteMessageAsync(entry.EntryChannelId, entry.EntryMessageId);
        }

        public async Task HandleAsync(MessagesBulkDeletedEventArgs args)
        {
            using var ctx = new AdminDatabaseContext(_provider);
            foreach (var id in args.Messages.Select(x => x.Id))
            {
                if (!(await ctx.Starboard.FirstOrDefaultAsync(x => x.MessageId == id
                                                                   || x.EntryMessageId == id) is { } entry))
                    continue;

                ctx.Starboard.Remove(entry);
                await ctx.SaveChangesAsync();

                if (id != entry.EntryMessageId)
                    await args.Client.DeleteMessageAsync(entry.EntryChannelId, entry.EntryMessageId);
            }
        }
    }
}