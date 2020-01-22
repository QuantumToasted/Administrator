using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Common.LocalizedEmbed;
using Administrator.Database;
using Administrator.Extensions;
using Administrator.Services;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Qmmands;
using Permission = Disqord.Permission;

namespace Administrator.Commands.Starboard
{
    [Name("Starboard")]
    [Group("star")]
    [RequireContext(ContextType.Guild)]
    public class StarboardCommands : AdminModuleBase
    {
        public Random Random { get; set; }

        public PaginationService Pagination { get; set; }

        [Command("", "show")]
        public async ValueTask<AdminCommandResult> ShowEntryAsync(ulong? messageId = null)
        {
            StarboardEntry entry;
            if (messageId.HasValue)
            {
                entry = await Context.Database.Starboard.FirstOrDefaultAsync(x =>
                    x.MessageId == messageId && x.GuildId == Context.Guild.Id);
            }
            else
            {
                var entries = await Context.Database.Starboard.Where(x => x.GuildId == Context.Guild.Id).ToListAsync();
                if (entries.Count == 0)
                    return CommandErrorLocalized("starboard_no_entry");

                entry = entries.GetRandomElement(Random);
            }

            if (entry is null)
                return CommandErrorLocalized("starboard_no_entry_id");

            var star = await Context.Database.GetSpecialEmojiAsync(Context.Guild.Id, EmojiType.Star);
            var channel = Context.Guild.GetTextChannel(entry.ChannelId);
            var message = (IUserMessage) await Context.Client.GetMessageAsync(entry.ChannelId, entry.MessageId);

            return CommandSuccess(
                $"{star}{entry.Stars.Count} `#{channel.Name}` {Localize("info_id")}: `{entry.MessageId}`",
                new LocalEmbedBuilder()
                    .WithAuthor(message.Author)
                    .WithDescription(new StringBuilder()
                        .AppendNewline((message.Content ?? message.Embeds.FirstOrDefault()?.Description)?
                            .TrimTo(LocalEmbedBuilder.MAX_DESCRIPTION_LENGTH - 50))
                        .AppendNewline()
                        .AppendNewline(Markdown.Link(Localize("info_jumpmessage"), entry.JumpUrl))
                        .ToString())
                    .WithImageUrl(
                        message.Attachments.FirstOrDefault(x => x.FileName.HasImageExtension(out _))?.Url ??
                        message.Embeds.FirstOrDefault()?.Image?.Url)
                    .WithTimestamp(message.Id.CreatedAt)
                    .Build());
        }

        [Command("stats")]
        public async ValueTask<AdminCommandResult> GetStatsAsync()
        {
            var entries = await Context.Database.Starboard
                .Where(x => x.GuildId == Context.Guild.Id)
                .ToListAsync();

            if (entries.Count == 0)
                return CommandErrorLocalized("starboard_no_entry");

            var star = await Context.Database.GetSpecialEmojiAsync(Context.Guild.Id, EmojiType.Star);

            var topStars = entries.OrderByDescending(x => x.Stars.Count)
                .Take(5);

            var topStarrees = entries.GroupBy(x => x.AuthorId)
                .OrderByDescending(x => x.Count())
                .Take(5)
                .ToDictionary(x => x.Key, x => x.Count());

            var topStarrers = entries.SelectMany(x => x.Stars)
                .GroupBy(x => x)
                .OrderByDescending(x => x.Count())
                .Take(5)
                .ToDictionary(x => x.Key, x => x.Count());

            return CommandSuccess(embed: new LocalizedEmbedBuilder(this)
                .WithLocalizedTitle("starboard_stats_title", Context.Guild.Name.Sanitize())
                .WithLocalizedDescription("starboard_stats_description", entries.Count,
                    entries.SelectMany(x => x.Stars).Count())
                .AddField(new LocalizedFieldBuilder(this)
                    .WithLocalizedName("starboard_stats_top_stars")
                    .WithValue(string.Join('\n', topStars.Select(x => $"{x.Stars.Count} {star} - `{x.MessageId}`"))))
                .AddField(new LocalizedFieldBuilder(this)
                    .WithLocalizedName("starboard_stats_top_starrees")
                    .WithValue(string.Join('\n', topStarrees.Select(x => $"{x.Value} {star} - <@{x.Key}>"))))
                .AddField(new LocalizedFieldBuilder(this)
                    .WithLocalizedName("starboard_stats_top_starrers")
                    .WithValue(string.Join('\n', topStarrers.Select(x => $"{x.Value} {star} - <@{x.Key}>"))))
                .Build());
        }

        [RequireUserPermissions(Permission.ManageGuild)]
        public sealed class ManageStarboardCommands : StarboardCommands
        {
            [Command("emoji")]
            public async ValueTask<AdminCommandResult> SetStarAsync(IEmoji emoji)
            {
                if (await Context.Database.SpecialEmojis.FindAsync(Context.Guild.Id.RawValue, EmojiType.Star) is { }
                    specialEmoji)
                {
                    if (specialEmoji.Emoji.Equals(emoji))
                        return CommandSuccessLocalized("starboard_emoji_update_same");

                    specialEmoji.Emoji = emoji;
                    Context.Database.SpecialEmojis.Update(specialEmoji);
                }
                else
                {
                    if (new LocalEmoji(EmojiType.Star.GetDescription()).Equals(emoji))
                        return CommandSuccessLocalized("starboard_emoji_update_same");

                    Context.Database.SpecialEmojis.Add(new SpecialEmoji(Context.Guild.Id, EmojiType.Star, emoji));
                }

                Context.Database.Starboard.RemoveRange(Context.Database.Starboard);
                await Context.Database.SaveChangesAsync();

                return CommandSuccessLocalized("starboard_emoji_update", args: emoji);
            }

            [Command("emoji")]
            public async ValueTask<AdminCommandResult> GetStarAsync()
                => CommandSuccessLocalized("starboard_emoji",
                    args: await Context.Database.GetSpecialEmojiAsync(Context.Guild.Id, EmojiType.Star));

            [Command("channel")]
            public async ValueTask<AdminCommandResult> SetChannelAsync(CachedTextChannel channel)
            {
                if (await Context.Database.LoggingChannels.FindAsync(Context.Guild.Id.RawValue, 
                        LogType.Starboard) is { } starboardChannel)
                {
                    if (starboardChannel.Id == channel.Id)
                        return CommandErrorLocalized("starboard_channel_same");

                    starboardChannel.Id = channel.Id;
                    Context.Database.LoggingChannels.Update(starboardChannel);
                    await Context.Database.SaveChangesAsync();

                    return CommandSuccessLocalized("starboard_channel_update", args: channel.Format());
                }

                Context.Database.LoggingChannels.Add(new LoggingChannel(channel.Id, Context.Guild.Id,
                    LogType.Starboard));
                await Context.Database.SaveChangesAsync();

                return CommandSuccessLocalized("starboard_channel_set", args: channel.Format());
            }

            [Command("channel")]
            public async ValueTask<AdminCommandResult> GetChannelAsync()
            {
                if (!(await Context.Database.GetLoggingChannelAsync(Context.Guild.Id,
                    LogType.Starboard) is { } starboardChannel))
                    return CommandErrorLocalized("starboard_channel_none");

                return CommandSuccessLocalized("starboard_channel",
                    args: Context.Guild.GetTextChannel(starboardChannel.Id).Format());
            }

            [Command("minimum")]
            public async ValueTask<AdminCommandResult> SetMinimumStarsAsync([MustBe(Operator.GreaterThan, 0)] int count)
            {
                var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
                guild.MinimumStars = count;
                Context.Database.Guilds.Update(guild);
                await Context.Database.SaveChangesAsync();

                return CommandSuccessLocalized("starboard_minimum_update", args: Markdown.Bold(guild.MinimumStars.ToString()));
            }

            [Command("minimum")]
            public async ValueTask<AdminCommandResult> GetMinimumStarsAsync()
            {
                var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
                return CommandSuccessLocalized("starboard_minimum", args: Markdown.Bold(guild.MinimumStars.ToString()));
            }
        }

        [Group("blacklist")]
        [RequireUserPermissions(Permission.ManageMessages)]
        public sealed class StarboardBlacklistCommands : StarboardCommands
        {
            [Command]
            public async ValueTask<AdminCommandResult> ViewBlacklistAsync()
            {
                var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
                if (guild.BlacklistedStarboardIds.Count == 0)
                    return CommandErrorLocalized("starboard_blacklist_none");

                var list = new List<string>();
                foreach (var id in guild.BlacklistedStarboardIds)
                {
                    if (Context.Guild.GetTextChannel(id) is { } channel)
                    {
                        list.Add(channel.Format(false));
                        continue;
                    }
                    
                    var user = await Context.Client.GetOrDownloadUserAsync(id);
                    list.Add(user?.Format(false) ?? $"??? `{id}`");
                }

                var pages = DefaultPaginator.GeneratePages(list, lineFunc: str => str,
                    builder: new LocalizedEmbedBuilder(this)
                        .WithSuccessColor()
                        .WithLocalizedTitle("starboard_blacklist_title",
                        Context.Guild.Name.Sanitize()));

                if (pages.Count > 1)
                {
                    await Pagination.SendPaginatorAsync(Context.Channel, new DefaultPaginator(pages, 0), pages[0]);
                    return CommandSuccess();
                }

                return CommandSuccess(embed: pages[0].Embed);
            }

            [Command("add")]
            public ValueTask<AdminCommandResult> BlacklistAsync([Remainder] CachedMember member)
                => BlacklistAsync(member.Id, true);

            [Command("add")]
            public ValueTask<AdminCommandResult> BlacklistAsync(CachedTextChannel channel)
                => BlacklistAsync(channel.Id, false);

            private async ValueTask<AdminCommandResult> BlacklistAsync(ulong id, bool isUser)
            {
                var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
                if (!guild.BlacklistedStarboardIds.Contains(id))
                    guild.BlacklistedStarboardIds.Add(id);

                Context.Database.Guilds.Update(guild);
                await Context.Database.SaveChangesAsync();

                return CommandSuccessLocalized("starboard_blacklist_add",
                    args: isUser ? Context.Guild.GetMember(id).Format() : Context.Guild.GetTextChannel(id).Format());
            }

            [Command("remove")]
            public ValueTask<AdminCommandResult> RemoveFromBlacklistAsync([Remainder] CachedMember member)
                => RemoveFromBlacklistAsync(member.Id, true);

            [Command("remove")]
            public ValueTask<AdminCommandResult> RemoveFromBlacklistAsync(CachedTextChannel channel)
                => RemoveFromBlacklistAsync(channel.Id, false);

            private async ValueTask<AdminCommandResult> RemoveFromBlacklistAsync(ulong id, bool isUser)
            {
                var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
                guild.BlacklistedStarboardIds.Remove(id);

                Context.Database.Guilds.Update(guild);
                await Context.Database.SaveChangesAsync();

                return CommandSuccessLocalized("starboard_blacklist_remove",
                    args: isUser ? Context.Guild.GetMember(id).Format() : Context.Guild.GetTextChannel(id).Format());
            }
        }
    }
}