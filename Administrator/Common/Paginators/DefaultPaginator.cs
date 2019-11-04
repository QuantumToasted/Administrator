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
        private readonly CancellationTokenSource _expiryToken;
        private int _currentPage;

        public DefaultPaginator(IUserMessage message, List<Page> pages, int currentPage, PaginationService service)
            : base(message, new IEmote[] {Left, Right}, service)
        {
            _pages = pages;
            _currentPage = currentPage;
            _timer = new Timer(Expire, this, TimeSpan.FromSeconds(30), TimeSpan.FromMilliseconds(-1));
            _expiryToken = new CancellationTokenSource();
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

        public override async ValueTask DisposeAsync()
        {
            await _timer.DisposeAsync();
            await base.DisposeAsync();
        }

        public Task WaitForExpiryAsync()
            => Task.Delay(-1, _expiryToken.Token);

        public static List<Page> GeneratePages<T>(List<T> list, int maxLength = EmbedBuilder.MaxDescriptionLength,
            Func<T, string> lineFunc = null, Func<string> plaintextFunc = null, Func<EmbedBuilder, EmbedBuilder> embedFunc = null)
        {
            var pages = new List<Page>();

            var builder = new StringBuilder();
            var embedBuilder = embedFunc?.Invoke(new EmbedBuilder()) ?? new EmbedBuilder();
            for (var i = 0; i < list.Count; i++)
            {
                var entry = list[i];
                var text = lineFunc?.Invoke(entry) ?? entry.ToString();
                if (builder.Length + text.Length > maxLength)
                {
                    pages.Add(new Page(plaintextFunc?.Invoke(),
                        embedBuilder
                        .WithDescription(builder.ToString()).Build()));

                    builder.Clear().AppendLine(text);
                }
                else if (i == list.Count - 1)
                {
                    pages.Add(new Page(plaintextFunc?.Invoke(),
                        embedBuilder
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
            Func<string> plaintextFunc = null, Func<EmbedBuilder, EmbedBuilder> embedFunc = null)
        {
            var pages = new List<Page>();
            var split = list.SplitBy(numberPerPage);
            var builder = embedFunc?.Invoke(new EmbedBuilder()) ?? new EmbedBuilder();
            foreach (var group in split)
            {
                var text = plaintextFunc?.Invoke() ?? string.Empty;
                foreach (var item in group)
                {
                    builder.AddField(fieldFunc(item));
                }

                pages.Add(new Page(text, builder.Build()));
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

        private void Expire(object _)
        {
            _expiryToken.Cancel();
            _ = CloseAsync();
        }
    }
}