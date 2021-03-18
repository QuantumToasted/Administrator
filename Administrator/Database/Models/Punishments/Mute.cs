using System;
using Administrator.Common;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database
{
    public sealed class Mute : RevocablePunishment, IEntityTypeConfiguration<Mute>
    {
#if !MIGRATION_MODE
        public Mute(IGuild guild, IUser target, IUser moderator, ulong? channelId = null, IOverwrite previousOverwrite = null, 
            TimeSpan? duration = null, string reason = null, Upload attachment = null) 
            : base(guild, target, moderator, duration, reason, attachment)
        {
            ChannelId = channelId;
            PreviousChannelAllowValue = previousOverwrite?.Permissions.Allowed;
            PreviousChannelDenyValue = previousOverwrite?.Permissions.Denied;
        }
#endif

        public ulong? ChannelId { get; set; }

        public ulong? PreviousChannelAllowValue { get; set; }

        public ulong? PreviousChannelDenyValue { get; set; }

        void IEntityTypeConfiguration<Mute>.Configure(EntityTypeBuilder<Mute> builder)
        {
            builder.HasBaseType<RevocablePunishment>();
        }
    }
}