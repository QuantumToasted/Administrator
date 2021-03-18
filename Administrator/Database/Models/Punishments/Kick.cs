using Administrator.Common;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database
{
    public sealed class Kick : Punishment, IEntityTypeConfiguration<Kick>
    {
#if !MIGRATION_MODE
        public Kick(IGuild guild, IUser target, IUser moderator, string reason = null, Upload attachment = null) 
            : base(guild, target, moderator, reason, attachment)
        { }
#endif
        
        void IEntityTypeConfiguration<Kick>.Configure(EntityTypeBuilder<Kick> builder)
        {
            builder.HasBaseType<Punishment>();
        }
    }
}