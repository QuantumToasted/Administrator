using Administrator.Common;
using Disqord;

namespace Administrator.Database
{
    public sealed class SpecialEmoji
    {
        public ulong GuildId { get; set; }

        public EmojiType Type { get; set; }

        public IEmoji Emoji { get; set; }
    }
}