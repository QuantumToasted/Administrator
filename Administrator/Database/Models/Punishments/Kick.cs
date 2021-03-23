using System;
using Administrator.Common;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database
{
    public sealed class Kick : Punishment, IEntityTypeConfiguration<Kick>
    {
        public static Kick Create(IGuild guild, IUser target, IUser moderator, string reason = null, Upload attachment = null)
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
                CreatedAt = DateTimeOffset.UtcNow
            };
        }
        
        void IEntityTypeConfiguration<Kick>.Configure(EntityTypeBuilder<Kick> builder)
        {
            builder.HasBaseType<Punishment>();
        }
    }
}