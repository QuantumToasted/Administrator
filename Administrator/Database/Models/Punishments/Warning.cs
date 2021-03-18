using System;
using Administrator.Common;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database
{
    public sealed class Warning : RevocablePunishment, IEntityTypeConfiguration<Warning>
    {
#if !MIGRATION_MODE
        public Warning(IGuild guild, IUser target, IUser moderator, string reason = null, Upload attachment = null) 
            : base(guild, target, moderator, default, reason, attachment)
        { }
#endif
        
        public int? SecondaryPunishmentId { get; set; }

        public void SetSecondaryPunishment(Punishment otherPunishment)
        {
            SecondaryPunishmentId = otherPunishment.Id;
        }

        void IEntityTypeConfiguration<Warning>.Configure(EntityTypeBuilder<Warning> builder)
        {
            builder.HasBaseType<RevocablePunishment>();
        }
    }
}