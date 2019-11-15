using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Administrator.Extensions;
using Discord;

namespace Administrator.Common
{
    public sealed class DefaultPaginator : Paginator
    {
        private readonly List<Page> _pages;
        private readonly CancellationTokenSource _tokenSource;
        private int _currentPage;

        public DefaultPaginator(List<Page> pages, int currentPage)
            : base(new[] { EmoteTools.Left, EmoteTools.Right })
        {
            _pages = pages;
            _currentPage = currentPage;
            _tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            Task.Delay(-1, _tokenSource.Token).ContinueWith(_ => DisposeAsync());
        }

        public override async ValueTask DisposeAsync()
        {
            _tokenSource.Dispose();
            _service.RemovePaginator(this);

            if (!_isPrivateMessage)
            {
                await Message.RemoveAllReactionsAsync();
            }      
        }

        public override async ValueTask<Page> GetPageAsync(IEmote emote, IUser user)
        {
            if (!_isPrivateMessage)
            {
                await Message.RemoveReactionAsync(emote, user);
            }

            if (emote.Equals(EmoteTools.Left) && _currentPage > 0)
            {
                _tokenSource.CancelAfter(TimeSpan.FromSeconds(30));
                return _pages[--_currentPage];
            }

            if (emote.Equals(EmoteTools.Right) && _currentPage < _pages.Count - 1)
            {
                _tokenSource.CancelAfter(TimeSpan.FromSeconds(30));
                return _pages[++_currentPage];
            }
            
            return null;
        }

        public static List<Page> GeneratePages<T>(List<T> list, int maxLength = EmbedBuilder.MaxDescriptionLength,
            Func<T, string> lineFunc = null, Func<string> plaintextFunc = null, EmbedBuilder builder = null)
        {
            var pages = new List<Page>();

            var sb = new StringBuilder();
            builder ??= new EmbedBuilder();
            for (var i = 0; i < list.Count; i++)
            {
                var entry = list[i];
                var text = lineFunc?.Invoke(entry) ?? entry.ToString();
                if (builder.Length + text.Length + 1 > maxLength) // +1 to account for \n
                {
                    pages.Add(new Page(plaintextFunc?.Invoke(),
                        builder
                        .WithDescription(builder.ToString()).Build()));

                    sb.Clear().AppendLine(text);
                }
                else if (i == list.Count - 1)
                {
                    pages.Add(new Page(plaintextFunc?.Invoke(),
                        builder
                        .WithDescription(sb.AppendLine(text).ToString())
                        .Build()));
                }
                else
                {
                    sb.AppendLine(text);
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
            Func<string> plaintextFunc = null, EmbedBuilder builder = null)
        {
            var pages = new List<Page>();
            var split = list.SplitBy(numberPerPage);
            builder ??= new EmbedBuilder();
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
    }
}