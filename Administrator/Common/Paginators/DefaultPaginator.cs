using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Administrator.Extensions;
using Disqord;

namespace Administrator.Common
{
    public sealed class DefaultPaginator : Paginator
    {
        private readonly List<Page> _pages;
        private readonly CancellationTokenSource _tokenSource;
        private int _currentPage;

        public DefaultPaginator(List<Page> pages, int currentPage)
            : base(new[] { EmojiTools.Left, EmojiTools.Right })
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
                await Message.ClearReactionsAsync();
            }      
        }

        public override async ValueTask<Page> GetPageAsync(IEmoji emoji, Snowflake userId)
        {
            if (!_isPrivateMessage)
            {
                await Message.RemoveMemberReactionAsync(userId, emoji);
            }

            if (emoji.Equals(EmojiTools.Left) && _currentPage > 0)
            {
                _tokenSource.CancelAfter(TimeSpan.FromSeconds(30));
                return _pages[--_currentPage];
            }

            if (emoji.Equals(EmojiTools.Right) && _currentPage < _pages.Count - 1)
            {
                _tokenSource.CancelAfter(TimeSpan.FromSeconds(30));
                return _pages[++_currentPage];
            }
            
            return null;
        }

        public static List<Page> GeneratePages<T>(List<T> list, int maxLength = LocalEmbedBuilder.MAX_DESCRIPTION_LENGTH,
            Func<T, string> lineFunc = null, Func<string> plaintextFunc = null, Func<LocalEmbedBuilder> builderFunc = null)
        {
            var pages = new List<(string Plaintext, LocalEmbedBuilder Builder)>();

            var sb = new StringBuilder();
            var builder = builderFunc?.Invoke() ?? new LocalEmbedBuilder();
            for (var i = 0; i < list.Count; i++)
            {
                var entry = list[i];
                var text = lineFunc?.Invoke(entry) ?? entry.ToString();
                if (sb.Length + text.Length + 1 > maxLength) // +1 to account for \n
                {
                    pages.Add((plaintextFunc?.Invoke(),
                        builder
                        .WithDescription(builder.ToString())));

                    sb.Clear().AppendNewline(text);
                }
                else if (i == list.Count - 1)
                {
                    pages.Add((plaintextFunc?.Invoke(),
                        builder
                        .WithDescription(sb.AppendNewline(text).ToString())));
                }
                else
                {
                    sb.AppendNewline(text);
                }
            }
            
            if (pages.Count > 1)
            {
                for (var i = 0; i < pages.Count; i++)
                {
                    pages[i] = (pages[i].Plaintext,
                        pages[i].Builder.WithFooter($"{i + 1}/{pages.Count}"));
                }
            }

            return pages.Select(x => new Page(x.Plaintext, x.Builder.Build())).ToList();
        }

        public static List<Page> GeneratePages<T>(List<T> list, int numberPerPage, Func<T, LocalEmbedFieldBuilder> fieldFunc,
            Func<string> plaintextFunc = null, Func<LocalEmbedBuilder> builderFunc = null)
        {
            var pages = new List<(string Plaintext, LocalEmbedBuilder Builder)>();
            var split = list.SplitBy(numberPerPage);

            foreach (var group in split)
            {
                var builder = builderFunc?.Invoke() ?? new LocalEmbedBuilder();

                var text = plaintextFunc?.Invoke() ?? string.Empty;
                foreach (var item in group)
                {
                    builder.AddField(fieldFunc(item));
                }

                pages.Add((text, builder));
            }

            if (pages.Count > 1)
            {
                for (var i = 0; i < pages.Count; i++)
                {
                    pages[i] = (pages[i].Plaintext,
                        pages[i].Builder.WithFooter($"{i + 1}/{pages.Count}"));
                }
            }

            return pages.Select(x => new Page(x.Plaintext, x.Builder.Build())).ToList();
        }
    }
}