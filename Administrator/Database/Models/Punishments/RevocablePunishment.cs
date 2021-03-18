using System;
using Administrator.Common;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database
{
    public abstract class RevocablePunishment : Punishment, IEntityTypeConfiguration<RevocablePunishment>
    {
#if !MIGRATION_MODE
        protected RevocablePunishment(IGuild guild, IUser target, IUser moderator, TimeSpan? duration = null, string reason = null, Upload attachment = null)
            : base(guild, target, moderator, reason, attachment)
        {
            Expires = CreatedAt + duration;
        }
#endif
        
        public DateTimeOffset? RevokedAt { get; set; }

        public Snowflake RevokerId { get; set; }

        public string RevokerTag { get; set; }

        public string RevocationReason { get; set; }

        public DateTimeOffset? AppealedAt { get; set; }

        public string AppealReason { get; set; }

        public DateTimeOffset? Expires { get; set; }

        public bool IsExpired => DateTimeOffset.UtcNow > Expires;

        public void Revoke(IUser revoker, string reason)
        {
            RevokedAt = DateTimeOffset.UtcNow;
            RevokerId = revoker.Id;
            RevokerTag = revoker.Tag;
            RevocationReason = reason;
        }
        
        public void Appeal(string reason)
        {
            AppealedAt = DateTimeOffset.UtcNow;
            AppealReason = reason;
        }
        
        void IEntityTypeConfiguration<RevocablePunishment>.Configure(EntityTypeBuilder<RevocablePunishment> builder)
        {
            builder.HasBaseType<Punishment>();
        }
    }
}