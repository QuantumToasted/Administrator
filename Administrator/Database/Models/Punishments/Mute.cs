using System;
using System.IO;
using Disqord;

namespace Administrator.Database
{
    public sealed class Mute : RevocablePunishment
    {
        public Mute(ulong guildId, ulong targetId, ulong moderatorId, string reason, TimeSpan? duration, ulong? channelId, MemoryStream image = null, ImageFormat format = ImageFormat.Default)
            : base(guildId, targetId, moderatorId, reason, !duration.HasValue, image, format)
        {
            Duration = duration;
            ChannelId = channelId;
        }

        public TimeSpan? Duration { get; set; }

        public bool IsExpired => Duration.HasValue && DateTimeOffset.UtcNow > CreatedAt + Duration.Value;

        public ulong? ChannelId { get; set; }

        public void StoreOverwrite(CachedOverwrite overwrite)
        {
            PreviousChannelAllowValue = overwrite.Permissions.Allowed.RawValue;
            PreviousChannelDenyValue = overwrite.Permissions.Denied.RawValue;
        }

        public ulong? PreviousChannelAllowValue { get; set; }

        public ulong? PreviousChannelDenyValue { get; set; }
    }
}