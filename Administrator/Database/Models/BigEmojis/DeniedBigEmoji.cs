using System;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database
{
    public sealed class DeniedBigEmoji : BigEmoji,
        IEntityTypeConfiguration<DeniedBigEmoji>
    {
#if !MIGRATION_MODE
        public DeniedBigEmoji(RequestedBigEmoji emoji, IUser denier)
            : base(emoji.GuildId, emoji.Id, emoji.Name, emoji.IsAnimated)
        {
            DenierId = denier.Id;
            DenierTag = denier.Tag;
            DeniedAt = DateTimeOffset.UtcNow;
        }
#endif
        public Snowflake DenierId { get; set; }
        
        public string DenierTag { get; set; }
        
        public DateTimeOffset DeniedAt { get; set; }
        
        void IEntityTypeConfiguration<DeniedBigEmoji>.Configure(EntityTypeBuilder<DeniedBigEmoji> builder)
        {
            builder.HasBaseType<BigEmoji>();
        }
    }
}