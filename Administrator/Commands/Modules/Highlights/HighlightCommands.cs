using Qmmands;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Administrator.Services;
using Administrator.Extensions;
using Administrator.Common;
using System;
using Administrator.Database;
using System.Collections.Generic;
using Disqord;

namespace Administrator.Commands
{
    [Group("highlights")]
    [Name("Highlights")]
    public class HighlightCommands : AdminModuleBase
    {
        public PaginationService Pagination { get; set; }

        [Command("", "list")]
        public async ValueTask<AdminCommandResult> ListHighlightsAsync()
        {
            var highlights = await (Context.IsPrivate
                ? Context.Database.Highlights.Where(x => x.UserId == Context.User.Id && !x.GuildId.HasValue)
                : Context.Database.Highlights.Where(x => x.UserId == Context.User.Id && x.GuildId == Context.Guild.Id)).ToListAsync();
            /*
            var highlights = await Context.Database.Highlights
                .Where(x => x.UserId == Context.User.Id &&
                (Context.IsPrivate && !x.GuildId.HasValue || x.GuildId == Context.Guild.Id))
                .ToListAsync();
            */

            if (highlights.Count == 0)
                return Context.IsPrivate
                    ? CommandErrorLocalized("highlight_list_none_global")
                    : CommandErrorLocalized("highlight_list_none_server", args: Context.Guild.Name.Sanitize());

            var pages = DefaultPaginator.GeneratePages(highlights, lineFunc: highlight => $"{highlight.Id} - \"{highlight.Text}\"", 
                builder: new LocalEmbedBuilder().WithSuccessColor().WithTitle(Context.IsPrivate
                    ? Localize("highlight_list_global")
                    : Localize("highlight_list_guild", Context.Guild.Name.Sanitize())));

            if (pages.Count > 1)
            {
                await Pagination.SendPaginatorAsync(Context.Channel, new DefaultPaginator(pages, 0), pages[0]);
                return CommandSuccess();
            }

            return CommandSuccess(embed: pages[0].Embed);
        }

        [Command("add")]
        public async ValueTask<AdminCommandResult> AddHighlightAsync([Remainder] string text)
        {
            text = text.Trim().ToLowerInvariant();
            if (await Context.Database.Highlights
                .AnyAsync(x => x.UserId == Context.User.Id &&
                               (Context.IsPrivate && !x.GuildId.HasValue ||
                                x.GuildId == Context.Guild.Id) &&
                               text.Equals(x.Text)))
                return CommandErrorLocalized("highlight_add_exists");

            var highlight = Context.Database.Highlights.Add(new Highlight(Context.User.Id, text.Trim().ToLowerInvariant(), Context.Guild?.Id)).Entity;
            await Context.Database.SaveChangesAsync();

            return Context.IsPrivate
                ? CommandSuccessLocalized("highlight_add_global", args: Markdown.Code($"[#{highlight.Id}]"))
                : CommandSuccessLocalized("highlight_add_guild", args: new object[] { Markdown.Code($"[#{highlight.Id}]"), Context.Guild.Name.Sanitize()});
        }

        [Command("remove")]
        [Priority(0)]
        public async ValueTask<AdminCommandResult> RemoveHighlightAsync([Remainder] string text)
        {
            if (!(await Context.Database.Highlights.FirstOrDefaultAsync(x => x.UserId == Context.User.Id &&
                (Context.IsPrivate && !x.GuildId.HasValue || x.GuildId == Context.Guild.Id) &&
                x.Text.Equals(text, StringComparison.OrdinalIgnoreCase)) is { } highlight))
                return CommandErrorLocalized("highlight_remove_notfound");

            Context.Database.Highlights.Remove(highlight);
            await Context.Database.SaveChangesAsync();

            return Context.IsPrivate
                ? CommandSuccessLocalized("highlight_remove_global")
                : CommandSuccessLocalized("highlight_remove_guild", args: Context.Guild.Name.Sanitize());
        }

        [Command("remove")]
        [Priority(1)]
        public async ValueTask<AdminCommandResult> RemoveHighlightAsync(int id)
        {
            if (!(await Context.Database.Highlights.FirstOrDefaultAsync(x => x.UserId == Context.User.Id &&
                x.Id == id) is { } highlight))
                return CommandErrorLocalized("highlight_remove_notfound");

            Context.Database.Highlights.Remove(highlight);
            await Context.Database.SaveChangesAsync();

            return Context.IsPrivate
                ? CommandSuccessLocalized("highlight_remove_global")
                : CommandSuccessLocalized("highlight_remove_guild", args: Context.Guild.Name.Sanitize());
        }

        [Command("clear")]
        public async ValueTask<AdminCommandResult> ClearHighlightsAsync()
        {
            var highlights = await (Context.IsPrivate
                ? Context.Database.Highlights.Where(x => x.UserId == Context.User.Id && !x.GuildId.HasValue)
                : Context.Database.Highlights.Where(x => x.UserId == Context.User.Id && x.GuildId == Context.Guild.Id)).ToListAsync();

            if (highlights.Count == 0)
                return Context.IsPrivate
                    ? CommandErrorLocalized("highlight_list_none_global")
                    : CommandErrorLocalized("highlight_list_none_server", args: Context.Guild.Name.Sanitize());

            Context.Database.Highlights.RemoveRange(highlights);
            await Context.Database.SaveChangesAsync();

            return CommandSuccessLocalized("highlight_cleared");
        }

        [Group("blacklist")]
        public class HighlightBlacklistCommands : HighlightCommands
        {
            [Command]
            public async ValueTask<AdminCommandResult> ViewHighlightBlacklistAsync()
            {
                var user = await Context.Database.GetOrCreateGlobalUserAsync(Context.User.Id);
                
                var blacklist = new List<string>();
                var updated = false;
                foreach (var id in user.HighlightBlacklist)
                {
                    var target = await Context.Client.GetOrDownloadUserAsync(id);
                    if (target is null)
                    {
                        if (Context.Client.GetChannel(id) is CachedTextChannel channel)
                        {
                            blacklist.Add(channel.Format());
                        }
                        else
                        {
                            user.HighlightBlacklist.Remove(id);
                            updated = true;
                        }
                    }
                    else
                    {
                        blacklist.Add(target.Format());
                    }
                }

                if (updated)
                {
                    Context.Database.GlobalUsers.Update(user);
                    await Context.Database.SaveChangesAsync();
                }

                if (user.HighlightBlacklist.Count == 0)
                    return CommandErrorLocalized("highlight_blacklist_empty");

                var pages = DefaultPaginator.GeneratePages(blacklist, 
                    lineFunc: target => target, 
                    builder: new LocalEmbedBuilder().WithSuccessColor().WithTitle(Localize("highlight_blacklist")));

                if (pages.Count > 1)
                {
                    await Pagination.SendPaginatorAsync(Context.Channel, new DefaultPaginator(pages, 0), pages[0]);
                    return CommandSuccess();
                }

                return CommandSuccess(embed: pages[0].Embed);
            }

            [Command("add")]
            [RequireContext(ContextType.Guild)]
            public ValueTask<AdminCommandResult> BlacklistUserHighlightsAsync([Remainder] CachedMember user)
                => BlacklistUserHighlightsAsync((IUser) user);

            [Command("add")]
            [RequireContext(ContextType.Guild)]
            public async ValueTask<AdminCommandResult> BlacklistChannelHighlightsAsync([Remainder] CachedTextChannel channel)
            {
                if (channel.Guild.GetMember(Context.User.Id) is null)
                    return CommandErrorLocalized("highlight_blacklist_notmember");

                var user = await Context.Database.GetOrCreateGlobalUserAsync(Context.User.Id);

                if (!user.HighlightBlacklist.Contains(channel.Id))
                    user.HighlightBlacklist.Add(channel.Id);

                Context.Database.GlobalUsers.Update(user);
                await Context.Database.SaveChangesAsync();

                return CommandSuccessLocalized("highlight_blacklist_add", args: channel.Format());
            }

            [Command("add")]
            public async ValueTask<AdminCommandResult> BlacklistUserHighlightsAsync(ulong targetId)
            {
                var user = await Context.Client.GetOrDownloadUserAsync(targetId);
                if (user is null)
                {
                    if (Context.Client.GetChannel(targetId) is CachedTextChannel channel)
                    {
                        return await BlacklistChannelHighlightsAsync(channel);
                    }

                    return CommandErrorLocalized("channelparser_notfound");
                }

                return await BlacklistUserHighlightsAsync(user);
            }

            private async ValueTask<AdminCommandResult> BlacklistUserHighlightsAsync(IUser target)
            {
                var user = await Context.Database.GetOrCreateGlobalUserAsync(Context.User.Id);

                if (!user.HighlightBlacklist.Contains(target.Id))
                    user.HighlightBlacklist.Add(target.Id);

                Context.Database.GlobalUsers.Update(user);
                await Context.Database.SaveChangesAsync();

                return CommandSuccessLocalized("highlight_blacklist_add", args: target.Format());
            }

            [Command("remove")]
            [RequireContext(ContextType.Guild)]
            public ValueTask<AdminCommandResult> UnblacklistUserHighlightsAsync([Remainder] CachedMember user)
                => UnblacklistUserHighlightsAsync((IUser)user);

            [Command("remove")]
            [RequireContext(ContextType.Guild)]
            public async ValueTask<AdminCommandResult> UnblacklistChannelHighlightsAsync([Remainder] CachedTextChannel channel)
            {
                if (channel.Guild.GetMember(Context.User.Id) is null)
                    return CommandErrorLocalized("highlight_blacklist_notmember");

                var user = await Context.Database.GetOrCreateGlobalUserAsync(Context.User.Id);

                user.HighlightBlacklist.Remove(channel.Id);

                Context.Database.GlobalUsers.Update(user);
                await Context.Database.SaveChangesAsync();

                return CommandSuccessLocalized("highlight_blacklist_remove", args: channel.Format());
            }

            [Command("remove")]
            public async ValueTask<AdminCommandResult> UnblacklistUserHighlightsAsync(ulong targetId)
            {
                var user = await Context.Client.GetOrDownloadUserAsync(targetId);
                if (user is null)
                {
                    if (Context.Client.GetChannel(targetId) is CachedTextChannel channel)
                    {
                        return await UnblacklistChannelHighlightsAsync(channel);
                    }

                    return CommandErrorLocalized("channelparser_notfound");
                }

                return await BlacklistUserHighlightsAsync(user);
            }

            private async ValueTask<AdminCommandResult> UnblacklistUserHighlightsAsync(IUser target)
            {
                var user = await Context.Database.GetOrCreateGlobalUserAsync(Context.User.Id);

                user.HighlightBlacklist.Remove(target.Id);

                Context.Database.GlobalUsers.Update(user);
                await Context.Database.SaveChangesAsync();

                return CommandSuccessLocalized("highlight_blacklist_remove", args: target.Format());
            }
        }
    }
}
