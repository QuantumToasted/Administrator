using System;
using System.IO;
using Disqord;

namespace Administrator.Database
{
    public sealed class Ban : RevocablePunishment
    {
        public Ban(ulong guildId, ulong targetId, ulong moderatorId, string reason, TimeSpan? duration, MemoryStream image = null, ImageFormat format = ImageFormat.Default)
            : base(guildId, targetId, moderatorId, reason, !duration.HasValue || duration.Value > TimeSpan.FromDays(1), image, format)
        {
            Duration = duration;
        }

        public TimeSpan? Duration { get; set; }

        public bool IsExpired => Duration.HasValue && DateTimeOffset.UtcNow > CreatedAt + Duration.Value;
    }
}