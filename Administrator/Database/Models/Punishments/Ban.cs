using System;

namespace Administrator.Database
{
    public sealed class Ban : RevocablePunishment
    {
        public Ban(ulong guildId, ulong targetId, ulong moderatorId, string reason, TimeSpan? duration)
            : base(guildId, targetId, moderatorId, reason, !duration.HasValue || duration.Value > TimeSpan.FromDays(1))
        {
            Duration = duration;
        }

        public TimeSpan? Duration { get; set; }

        public bool IsExpired => Duration.HasValue && DateTimeOffset.UtcNow > CreatedAt + Duration.Value;
    }
}