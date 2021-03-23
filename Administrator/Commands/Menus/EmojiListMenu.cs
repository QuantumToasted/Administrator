﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Administrator.Database;
using Administrator.Extensions;
using Administrator.Services;
using Disqord.Bot;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Extensions.Interactivity.Menus.Paged;
using Microsoft.Extensions.DependencyInjection;

namespace Administrator.Commands
{
    public sealed class EmojiListMenu : PagedMenu
    {
        private readonly DiscordBotBase _bot;

        public EmojiListMenu(DiscordCommandContext context, IList<ApprovedBigEmoji> bigEmojis, int currentPage, bool isOwner = false)
            : base(context.Author.Id, new EmojiListPageProvider(bigEmojis), false)
        {
            _bot = context.Bot;
            CurrentPageIndex = Math.Min(currentPage, bigEmojis.Count) - 1;

            AddButtonAsync(new Button(EmojiService.Names["arrow_left"], e => ChangePageAsync(CurrentPageIndex - 1), 0));
            AddButtonAsync(new Button(EmojiService.Names["arrow_right"], e => ChangePageAsync(CurrentPageIndex + 1), 1));

            if (isOwner)
            {
                AddButtonAsync(new Button(EmojiService.Names["wastebasket"], e => RemoveEmojiAsync(CurrentPageIndex), 2));
            }
        }

        public EmojiListMenu(DiscordCommandContext context, IList<ApprovedBigEmoji> bigEmojis, string startingName, bool isOwner = false)
            : this(context, bigEmojis, Math.Max(0, bigEmojis.FirstIndexOf(x =>
                x.Name.Equals(startingName, StringComparison.InvariantCultureIgnoreCase))) + 1, isOwner)
        { }

        public new EmojiListPageProvider PageProvider => (EmojiListPageProvider) base.PageProvider;

        private async Task RemoveEmojiAsync(int index)
        {
            var (builder, emoji, alreadyDenied) = PageProvider.List[index];

            if (alreadyDenied)
            {
                await ChangePageAsync(index + 1);
                return;
            }

            PageProvider.List[index] = (builder, emoji, true);

            using var scope = _bot.Services.CreateScope();
            await using var ctx = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
            
            var user = await _bot.GetOrFetchUserAsync(UserId);

            ctx.BigEmojis.Remove(emoji);
            await ctx.SaveChangesAsync();

            ctx.BigEmojis.Add(DeniedBigEmoji.Create(emoji, user));
            await ctx.SaveChangesAsync();
            
            await ChangePageAsync(index + 1);
        }
    }
}