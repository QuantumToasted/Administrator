using System.Collections.Generic;
using System.Threading.Tasks;
using Administrator.Database;
using Administrator.Extensions;
using Administrator.Services;
using Disqord;
using Disqord.Bot;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Extensions.Interactivity.Menus.Paged;
using Microsoft.Extensions.DependencyInjection;

namespace Administrator.Commands
{
    public sealed class EmojiRequestMenu : PagedMenu
    {
        public EmojiRequestMenu(Snowflake userId, IList<RequestedBigEmoji> emojis) 
            : base(userId, new EmojiRequestPageProvider(emojis), false)
        {
            AddButtonAsync(new Button(EmojiService.Names["arrow_left"], e => ChangePageAsync(CurrentPageIndex - 1), 0));
            AddButtonAsync(new Button(EmojiService.Names["arrow_right"], e => ChangePageAsync(CurrentPageIndex + 1), 1));
            AddButtonAsync(new Button(EmojiService.Names["white_check_mark"], e => ApproveOrDenyAsync(CurrentPageIndex, true), 2));
            AddButtonAsync(new Button(EmojiService.Names["x"], e => ApproveOrDenyAsync(CurrentPageIndex, false), 3));
        }

        public new EmojiRequestPageProvider PageProvider => (EmojiRequestPageProvider) base.PageProvider;

        public DiscordBotBase Bot => (DiscordBotBase) Client;
 
        private async Task ApproveOrDenyAsync(int index, bool approval)
        {
            var (builder, emoji, alreadyDenied) = PageProvider.List[index];

            if (alreadyDenied.HasValue)
            {
                return;
            }

            PageProvider.List[index] = (builder, emoji, approval);

            using var scope = Bot.Services.CreateScope();
            await using var ctx = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
            
            var user = await Bot.GetOrFetchUserAsync(UserId);

            if (approval)
            {
                await emoji.ApproveAsync(ctx, user);
            }
            else
            {
                await emoji.DenyAsync(ctx, user);
            }

            await ctx.SaveChangesAsync();

            await ChangePageAsync(index + 1);
        }
    }
}