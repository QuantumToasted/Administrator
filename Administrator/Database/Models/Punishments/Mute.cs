using System;
using Discord;

namespace Administrator.Database
{
    public sealed class Mute : RevocablePunishment
    {
        public Mute(ulong guildId, ulong targetId, ulong moderatorId, string reason, TimeSpan? duration, ulong? channelId)
            : base(guildId, targetId, moderatorId, reason, !duration.HasValue)
        {
            Duration = duration;
            ChannelId = channelId;
        }

        public TimeSpan? Duration { get; set; }

        public bool IsExpired => Duration.HasValue && DateTimeOffset.UtcNow > CreatedAt + Duration.Value;

        public ulong? ChannelId { get; set; }

        public void StoreOverwrite(Overwrite overwrite)
        {
            PreviousChannelAllowValue = overwrite.Permissions.AllowValue;
            PreviousChannelDenyValue = overwrite.Permissions.DenyValue;
        }

        public ulong? PreviousChannelAllowValue { get; set; }

        public ulong? PreviousChannelDenyValue { get; set; }
    }
}