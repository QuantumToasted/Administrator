using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Administrator.Database;
using Administrator.Extensions;
using Disqord;
using Disqord.Extensions.Interactivity.Menus.Paged;

namespace Administrator.Commands
{
    public sealed class EmojiListPageProvider : IPageProvider
    {
        public EmojiListPageProvider(IList<ApprovedBigEmoji> emojis)
        {
            List = new List<(Page Page, ApprovedBigEmoji Emoji, bool AlreadyDenied)>();

            emojis = emojis.OrderBy(x => x.Name).ThenBy(x => x.ApprovedAt).ToList();

            for (var i = 0; i < emojis.Count; i++)
            {
                var emoji = emojis[i];

                var builder = new LocalEmbedBuilder()
                    .WithSuccessColor()
                    .WithTitle(emoji.Name)
                    .WithImageUrl(Discord.Cdn.GetCustomEmojiUrl(emoji.Id, emoji.IsAnimated)) // TODO: ICustomEmoji#GetUrl()
                    .WithFooter($"{i + 1}/{emojis.Count}");

                List.Add((new Page("All globally whitelisted emojis", builder), emoji, false));
            }
        }

        public IList<(Page Page, ApprovedBigEmoji Emoji, bool AlreadyDenied)> List { get; }

        public ValueTask<Page> GetPageAsync(PagedMenu menu)
        {
            var (page, _, alreadyDenied) = List[menu.CurrentPageIndex];

            if (alreadyDenied)
            {
                page.Embed.Footer.WithText($"{menu.CurrentPageIndex + 1}/{PageCount} (Denied)");
            }

            return new ValueTask<Page>(page);
        }

        public int PageCount => List.Count;
    }
}