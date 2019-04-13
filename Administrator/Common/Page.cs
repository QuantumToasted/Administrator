using Discord;

namespace Administrator.Common
{
    public sealed class Page
    {
        public Page(string text, Embed embed)
        {
            Text = text ?? string.Empty;
            Embed = embed;
        }

        public string Text { get; }

        public Embed Embed { get; }

        public static implicit operator Page(Embed embed)
            => new Page(null, embed);
    }
}