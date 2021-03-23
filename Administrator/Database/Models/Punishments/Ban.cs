using System;
using Administrator.Common;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database
{
    public sealed class Ban : RevocablePunishment, IEntityTypeConfiguration<Ban>
    {
        public static Ban Create(IGuild guild, IUser target, IUser moderator, TimeSpan? duration = null, 
            string reason = null, Upload attachment = null)
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
                ExpiresAt = DateTimeOffset.UtcNow + duration
            };
        }
        
        void IEntityTypeConfiguration<Ban>.Configure(EntityTypeBuilder<Ban> builder)
        {
            builder.HasBaseType<RevocablePunishment>();
        }
    }
}