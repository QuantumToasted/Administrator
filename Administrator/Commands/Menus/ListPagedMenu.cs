using System;
using Administrator.Services;
using Disqord;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Extensions.Interactivity.Menus.Paged;

namespace Administrator.Commands
{
    public sealed class ListPagedMenu<T> : PagedMenu
    {
        public ListPagedMenu(Snowflake userId, ListPageProvider<T> pageProvider, int currentPage = 0)
            : base(userId, pageProvider, false)
        {
            CurrentPageIndex = Math.Min(currentPage, pageProvider.PageCount) - 1;

            if (PageProvider.PageCount > 1)
            {
                AddButtonAsync(new Button(EmojiService.Names["arrow_left"], _ => ChangePageAsync(CurrentPageIndex - 1), 0));
                AddButtonAsync(new Button(EmojiService.Names["arrow_right"], _ => ChangePageAsync(CurrentPageIndex + 1), 1));
            }
        }
    }
}