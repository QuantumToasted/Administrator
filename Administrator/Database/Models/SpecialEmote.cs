using Administrator.Common;
using Discord;

namespace Administrator.Database
{
    public sealed class SpecialEmote
    {
        public ulong GuildId { get; set; }

        public EmoteType Type { get; set; }

        public IEmote Emote { get; set; }
    }
}