using Disqord;

namespace Administrator.Database
{
    public abstract class Keyed
    {
        public int Id { get; set; }

        /// <summary>
        /// Returns the Id of this <see cref="Keyed"/> in the format [#Id] formatted via Markdown.Code.
        /// </summary>
        public sealed override string ToString()
            => Markdown.Code($"[#{Id}]");
    }
}