using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Administrator.Services;
using Disqord;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Extensions.Interactivity.Menus.Paged;

namespace Administrator.Extensions
{
    public static class EnumerableExtensions
    {
        public static PagedMenu ToPagedMenu<T>(this IList<T> list, Snowflake userId, 
            Func<T, LocalEmbedFieldBuilder> fieldFactory, int fieldsPerPage = 5, 
            Func<LocalEmbedBuilder> builderFactory = null, Func<string> plaintextFactory = null, int firstPage = 0)
        {
            var pages = new List<Page>();

            foreach (var group in list.SplitBy(fieldsPerPage))
            {
                var builder = builderFactory?.Invoke() ?? new LocalEmbedBuilder().WithSuccessColor();
                var plaintext = plaintextFactory?.Invoke();

                foreach (var item in group)
                {
                    builder.AddField(fieldFactory(item));
                }

                pages.Add(new Page(plaintext, builder));
            }

            var menu = new PagedMenu(userId, new DefaultPageProvider(pages), false)
            {
                CurrentPageIndex = firstPage,
                StopBehavior = StopBehavior.ClearReactions
            };

            menu.AddButtonAsync(new Button(EmojiService.Names["arrow_left"],
                e => menu.ChangePageAsync(menu.CurrentPageIndex - 1)));
            
            menu.AddButtonAsync(new Button(EmojiService.Names["arrow_right"],
                e => menu.ChangePageAsync(menu.CurrentPageIndex + 1)));

            return menu;
        }
        
        public static PagedMenu ToPagedMenu<T>(this IList<T> list, Snowflake userId, Func<T, string> lineFactory,
            int maxDescriptionLength = LocalEmbedBuilder.MAX_DESCRIPTION_LENGTH,
            Func<LocalEmbedBuilder> builderFactory = null, Func<string> plaintextFactory = null, int firstPage = 0)
        {
            var pages = new List<Page>();

            var sb = new StringBuilder();
            for (var i = 0; i < list.Count; i++)
            {
                var line = lineFactory(list[i]);

                if (line.Length + sb.Length > maxDescriptionLength)
                {
                    pages.Add(new Page(plaintextFactory?.Invoke(),
                        builderFactory?.Invoke() ?? new LocalEmbedBuilder()
                            .WithSuccessColor()
                            .WithDescription(sb.ToString())));

                    sb.Clear();
                }

                sb.AppendNewline(line);

                if (i == list.Count - 1)
                {
                    pages.Add(new Page(plaintextFactory?.Invoke(),
                        builderFactory?.Invoke() ?? new LocalEmbedBuilder()
                            .WithSuccessColor()
                            .WithDescription(sb.ToString())));
                }
            }

            for (var i = 0; i < pages.Count && pages.Count > 1; i++)
            {
                pages[i].Embed.WithFooter($"{i + 1}/{pages.Count}");
            }

            var menu = new PagedMenu(userId, new DefaultPageProvider(pages), false)
            {
                CurrentPageIndex = firstPage,
                StopBehavior = StopBehavior.ClearReactions
            };

            menu.AddButtonAsync(new Button(EmojiService.Names["arrow_left"],
                e => menu.ChangePageAsync(menu.CurrentPageIndex - 1)));
            
            menu.AddButtonAsync(new Button(EmojiService.Names["arrow_right"],
                e => menu.ChangePageAsync(menu.CurrentPageIndex + 1)));

            return menu;
        }
        
        public static int FirstIndexOf<T>(this IList<T> list, Func<T, bool> func)
        {
            for (var i = 0; i < list.Count; i++)
            {
                var t = list[i];
                if (func(t))
                    return i;
            }

            return -1;
        }
        
        public static IEnumerable<T> DistinctBy<T>(this IEnumerable<T> enumerable, Func<T, object> keySelector)
            => enumerable.GroupBy(keySelector).Select(x => x.FirstOrDefault());

        public static T GetRandomElement<T>(this IEnumerable<T> enumerable, Random random)
        {
            if (enumerable is not IList<T> list)
                list = enumerable.ToList();
            return list[random.Next(0, list.Count)];
        }
        
        public static List<List<T>> SplitBy<T>(this IEnumerable<T> enumerable, int count)
        {
            var newList = new List<List<T>>();

            var list = enumerable as List<T> ?? enumerable.ToList();

            if (list.Count <= count)
            {
                newList.Add(list);
            }
            else
            {
                for (var i = 0; i < list.Count; i += count)
                {
                    newList.Add(list.GetRange(i, Math.Min(count, list.Count - i)));
                }
            }

            return newList;
        }
    }
}