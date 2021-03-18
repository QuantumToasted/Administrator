using System;
using Administrator.Common;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database
{
    public sealed class Ban : RevocablePunishment, IEntityTypeConfiguration<Ban>
    {
#if !MIGRATION_MODE
        public Ban(IGuild guild, IUser target, IUser moderator, TimeSpan? duration = null, string reason = null, Upload attachment = null) 
            : base(guild, target, moderator, duration, reason, attachment)
        { }
#endif
        
        void IEntityTypeConfiguration<Ban>.Configure(EntityTypeBuilder<Ban> builder)
        {
            builder.HasBaseType<RevocablePunishment>();
        }
    }
}