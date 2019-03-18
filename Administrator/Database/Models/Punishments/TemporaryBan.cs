using System;

namespace Administrator.Database
{
    public sealed class TemporaryBan : RevocablePunishment
    {
        public TemporaryBan(ulong guildId, ulong targetId, ulong moderatorId, string reason, TimeSpan duration)
            : base(guildId, targetId, moderatorId, reason, duration > TimeSpan.FromDays(1))
        {
            Duration = duration;
        }

        public TimeSpan Duration { get; set; }
    }
}