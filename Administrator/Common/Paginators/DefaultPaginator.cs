using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Administrator.Extensions;
using Administrator.Services;
using Discord;

namespace Administrator.Common
{
    public sealed class DefaultPaginator : Paginator
    {
        private static readonly Emoji Left = new Emoji("⬅");
        private static readonly Emoji Right = new Emoji("➡");
        private readonly List<Page> _pages;
        private readonly Timer _timer;
        private int _currentPage;

        public DefaultPaginator(IUserMessage message, List<Page> pages, int currentPage, PaginationService service = null)
            : base(message, new IEmote[] {Left, Right}, service)
        {
            _pages = pages;
            _currentPage = currentPage;
            _timer = new Timer(Expire, this, TimeSpan.FromSeconds(30), TimeSpan.FromMilliseconds(-1));
        }

        public override ValueTask<Page> GetPageAsync(IUser user, IEmote emote)
        {
            if (emote.Equals(Left) && _currentPage > 0)
            {
                _timer.Change(TimeSpan.FromSeconds(30), TimeSpan.FromMilliseconds(-1));
                _ = Message.RemoveReactionAsync(emote, user);
                return new ValueTask<Page>(_pages[--_currentPage]);
            }

            if (emote.Equals(Right) && _currentPage < _pages.Count - 1)
            {
                _timer.Change(TimeSpan.FromSeconds(30), TimeSpan.FromMilliseconds(-1));
                _ = Message.RemoveReactionAsync(emote, user);
                return new ValueTask<Page>(_pages[++_currentPage]);
            }

            _ = Message.RemoveReactionAsync(emote, user);
            return new ValueTask<Page>((Page) null);
        }

        public override Task CloseAsync()
        {
             _ = Message?.RemoveAllReactionsAsync();
             return Task.CompletedTask;
        }

        public override void Dispose()
        {
            base.Dispose();
            _timer.Dispose();
        }

        public static List<Page> GeneratePages<T>(List<T> list, int maxLength = EmbedBuilder.MaxDescriptionLength,
            Func<T, string> lineFunc = null, Func<string> plaintextFunc = null, Func<EmbedBuilder> embedFunc = null)
        {
            var pages = new List<Page>();

            var builder = new StringBuilder();
            for (var i = 0; i < list.Count; i++)
            {
                var entry = list[i];
                var text = lineFunc?.Invoke(entry) ?? entry.ToString();
                if (builder.Length + text.Length > maxLength)
                {
                    pages.Add(new Page(plaintextFunc?.Invoke(),
                        (embedFunc?.Invoke() ?? new EmbedBuilder())
                        .WithDescription(builder.ToString()).Build()));

                    builder.Clear().AppendLine(text);
                }
                else if (i == list.Count - 1)
                {
                    pages.Add(new Page(plaintextFunc?.Invoke(),
                        (embedFunc?.Invoke() ?? new EmbedBuilder())
                        .WithDescription(builder.AppendLine(text).ToString())
                        .Build()));
                }
                else
                {
                    builder.AppendLine(text);
                }
            }
            
            if (pages.Count > 1)
            {
                for (var i = 0; i < pages.Count; i++)
                {
                    var page = pages[i];
                    pages[i] = new Page(page.Text,
                        page.Embed.ToEmbedBuilder().WithFooter($"{i + 1}/{pages.Count}").Build());
                }
            }

            return pages;
        }

        public static List<Page> GeneratePages<T>(List<T> list, int numberPerPage, Func<T, EmbedFieldBuilder> fieldFunc,
            Func<string> plaintextFunc = null, Func<EmbedBuilder> embedFunc = null)
        {
            var pages = new List<Page>();
            var split = list.SplitBy(numberPerPage);
            foreach (var group in split)
            {
                var text = plaintextFunc?.Invoke() ?? string.Empty;
                var builder = embedFunc?.Invoke() ?? new EmbedBuilder();
                foreach (var item in group)
                {
                    builder.AddField(fieldFunc(item));
                }

                pages.Add(new Page(text, builder.Build()));
            }

            return pages;
        }

        private void Expire(object state)
            => Dispose();
    }
}