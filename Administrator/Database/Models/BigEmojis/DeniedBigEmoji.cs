using System;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database
{
    public sealed class DeniedBigEmoji : BigEmoji,
        IEntityTypeConfiguration<DeniedBigEmoji>
    {
        public Snowflake DenierId { get; set; }
        
        public string DenierTag { get; set; }
        
        public DateTimeOffset DeniedAt { get; set; }

        public static DeniedBigEmoji Create(BigEmoji emoji, IUser denier)
        {
            return new()
            {
                Id = emoji.Id,
                Name = emoji.Name,
                IsAnimated = emoji.IsAnimated,
                GuildId = emoji.GuildId,
                DenierId = denier.Id,
                DenierTag = denier.Tag,
                DeniedAt = DateTimeOffset.UtcNow
            };
        }
        
        void IEntityTypeConfiguration<DeniedBigEmoji>.Configure(EntityTypeBuilder<DeniedBigEmoji> builder)
        {
            builder.HasBaseType<BigEmoji>();
        }
    }
}