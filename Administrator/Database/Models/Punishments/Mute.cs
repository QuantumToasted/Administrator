using System;
using Administrator.Common;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database
{
    public sealed class Mute : RevocablePunishment, IEntityTypeConfiguration<Mute>
    {
        public ulong? ChannelId { get; set; }

        public ulong? PreviousChannelAllowValue { get; set; }

        public ulong? PreviousChannelDenyValue { get; set; }

        public static Mute Create(IGuild guild, IUser target, IUser moderator, ulong? channelId = null, IOverwrite previousOverwrite = null, 
            TimeSpan? duration = null, string reason = null, Upload attachment = null)
        {
            return new()
            {
                GuildId = guild.Id,
                TargetId = target.Id,
                TargetTag = target.Tag,
                ModeratorId = moderator.Id,
                ModeratorTag = moderator.Tag,
                Reason = reason,
                Attachment = attachment,
                CreatedAt = DateTimeOffset.UtcNow,
                ChannelId = channelId,
                PreviousChannelAllowValue = previousOverwrite?.Permissions.Allowed,
                PreviousChannelDenyValue = previousOverwrite?.Permissions.Denied,
                ExpiresAt = DateTimeOffset.UtcNow + duration
            };
        }

        void IEntityTypeConfiguration<Mute>.Configure(EntityTypeBuilder<Mute> builder)
        {
            builder.HasBaseType<RevocablePunishment>();
        }
    }
}