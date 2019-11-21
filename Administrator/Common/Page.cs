using Disqord;

namespace Administrator.Common
{
    public sealed class Page
    {
        public Page(string text, LocalEmbed embed)
        {
            Text = text ?? string.Empty;
            Embed = embed;
        }

        public string Text { get; }

        public LocalEmbed Embed { get; }

        public static implicit operator Page(LocalEmbed embed)
            => new Page(null, embed);
    }
}