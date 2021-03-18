using Disqord;

namespace Administrator.Database
{
    public abstract class Keyed
    {
        public int Id { get; set; }

        public sealed override string ToString()
            => Markdown.Code($"[#{Id}]");
    }
}