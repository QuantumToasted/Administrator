using System;
using Administrator.Common;

namespace Administrator.Database
{
    public sealed class WarningPunishment
    {
        private WarningPunishment()
        { }

        public WarningPunishment(ulong guildId, int count, PunishmentType type, TimeSpan? duration)
        {
            GuildId = guildId;
            Count = count;
            Type = type;
            Duration = duration;
        }

        public ulong GuildId { get; set; }

        public int Count { get; set; }

        public PunishmentType Type { get; set; }

        public TimeSpan? Duration { get; set; }
    }
}