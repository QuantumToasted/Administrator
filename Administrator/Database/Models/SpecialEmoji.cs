using Administrator.Common;
using Disqord;

namespace Administrator.Database
{
    public sealed class SpecialEmoji
    {
        private SpecialEmoji()
        { }

        public SpecialEmoji(ulong guildId, EmojiType type, IEmoji emoji)
        {
            GuildId = guildId;
            Type = type;
            Emoji = emoji;
        }

        public ulong GuildId { get; set; }

        public EmojiType Type { get; set; }

        public IEmoji Emoji { get; set; }
    }
}