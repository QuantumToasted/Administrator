using System;

namespace Administrator.Database
{
    public sealed class Mute : RevocablePunishment
    {
        public Mute(ulong guildId, ulong targetId, ulong moderatorId, string reason, TimeSpan? duration)
            : base(guildId, targetId, moderatorId, reason, !duration.HasValue)
        {
            Duration = duration;
        }

        public TimeSpan? Duration { get; set; }

        public bool IsExpired => Duration.HasValue && DateTimeOffset.UtcNow > CreatedAt + Duration.Value;
    }
}