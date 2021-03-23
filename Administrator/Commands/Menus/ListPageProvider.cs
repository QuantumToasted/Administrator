using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Administrator.Extensions;
using Disqord;
using Disqord.Extensions.Interactivity.Menus.Paged;

namespace Administrator.Commands
{
    public sealed class ListPageProvider<T> : IPageProvider
    {
        public ListPageProvider(IList<T> list, Func<T, LocalEmbedFieldBuilder> fieldFactory, int fieldsPerPage = 5,
            Func<LocalEmbedBuilder> builderFactory = null, Func<string> plaintextFactory = null)
        {
            Pages = new List<Page>();

            foreach (var group in list.SplitBy(fieldsPerPage))
            {
                var builder = builderFactory?.Invoke() ?? new LocalEmbedBuilder().WithSuccessColor();
                var plaintext = plaintextFactory?.Invoke();

                foreach (var item in group)
                {
                    builder.AddField(fieldFactory(item));
                }

                Pages.Add(new Page(plaintext, builder));
            }
            
            for (var i = 0; i < Pages.Count && Pages.Count > 1; i++)
            {
                Pages[i].Embed.WithFooter($"{i + 1}/{Pages.Count}");
            }
        }

        public ListPageProvider(IList<T> list, Func<T, string> lineFactory, 
            int maxDescriptionLength = LocalEmbedBuilder.MAX_DESCRIPTION_LENGTH,
            Func<LocalEmbedBuilder> builderFactory = null, Func<string> plaintextFactory = null)
        {
            Pages = new List<Page>();

            var sb = new StringBuilder();
            for (var i = 0; i < list.Count; i++)
            {
                var line = lineFactory(list[i]);

                if (line.Length + sb.Length > maxDescriptionLength)
                {
                    Pages.Add(new Page(plaintextFactory?.Invoke(),
                        (builderFactory?.Invoke() ?? new LocalEmbedBuilder().WithSuccessColor()).WithDescription(
                            sb.ToString())));

                    sb.Clear();
                }

                sb.AppendNewline(line);

                if (i == list.Count - 1)
                {
                    Pages.Add(new Page(plaintextFactory?.Invoke(),
                        (builderFactory?.Invoke() ?? new LocalEmbedBuilder().WithSuccessColor()).WithDescription(
                            sb.ToString())));
                }
            }

            for (var i = 0; i < Pages.Count && Pages.Count > 1; i++)
            {
                Pages[i].Embed.WithFooter($"{i + 1}/{Pages.Count}");
            }
        }

        public IList<Page> Pages { get; }

        public ValueTask<Page> GetPageAsync(PagedMenu menu)
            => new(Pages[menu.CurrentPageIndex]);

        public int PageCount => Pages.Count;
    }
}