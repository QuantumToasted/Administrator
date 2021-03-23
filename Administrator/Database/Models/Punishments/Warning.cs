using System;
using Administrator.Common;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database
{
    public sealed class Warning : RevocablePunishment, IEntityTypeConfiguration<Warning>
    {
        public int? SecondaryPunishmentId { get; set; }

        public void SetSecondaryPunishment(Punishment otherPunishment)
        {
            SecondaryPunishmentId = otherPunishment.Id;
        }

        public static Warning Create(IGuild guild, IUser target, IUser moderator, string reason = null, Upload attachment = null)
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

        void IEntityTypeConfiguration<Warning>.Configure(EntityTypeBuilder<Warning> builder)
        {
            builder.HasBaseType<RevocablePunishment>();
        }
    }
}