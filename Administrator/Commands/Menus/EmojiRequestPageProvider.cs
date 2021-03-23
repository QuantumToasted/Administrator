using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Administrator.Database;
using Administrator.Extensions;
using Disqord;
using Disqord.Extensions.Interactivity.Menus.Paged;

namespace Administrator.Commands
{
    public sealed class EmojiRequestPageProvider : IPageProvider
    {
        public EmojiRequestPageProvider(IList<RequestedBigEmoji> emojis)
        {
            List = new List<(Page Page, RequestedBigEmoji Emoji, bool? IsApproved)>();

            emojis = emojis.OrderBy(x => x.Name).ThenBy(x => x.RequestedAt).ToList();

            for (var i = 0; i < emojis.Count; i++)
            {
                var emoji = emojis[i];

                var builder = new LocalEmbedBuilder()
                    .WithSuccessColor()
                    .WithTitle(emoji.Name)
                    .WithDescription($"Requested by {emoji.RequesterTag} (`{emoji.RequesterId}`)")
                    .WithImageUrl(Discord.Cdn.GetCustomEmojiUrl(emoji.Id, emoji.IsAnimated))
                    .WithFooter($"{i + 1}/{emojis.Count}");

                List.Add((new Page("All currently requested emojis", builder), emoji, null));
            }
        }

        public IList<(Page Page, RequestedBigEmoji Emoji, bool? IsApproved)> List { get; }

        public ValueTask<Page> GetPageAsync(PagedMenu menu)
        {
            var (page, _, isApproved) = List[menu.CurrentPageIndex];

            page.Embed.Footer.WithText(isApproved switch
            {
                true => $"{menu.CurrentPageIndex + 1}/{List.Count} (Approved)",
                false => $"{menu.CurrentPageIndex + 1}/{List.Count} (Denied)",
                null => page.Embed.Footer.Text
            });

            return new ValueTask<Page>(page);
        }

        public int PageCount => List.Count;
    }
}