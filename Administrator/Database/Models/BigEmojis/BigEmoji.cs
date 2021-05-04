using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database
{
    public abstract class BigEmoji : IEntityTypeConfiguration<BigEmoji>,
        IGuildDbEntity
    {
        public Snowflake Id { get; set; }

        public Snowflake GuildId { get; set; }

        public string Name { get; set; }

        public bool IsAnimated { get; set; }

        void IEntityTypeConfiguration<BigEmoji>.Configure(EntityTypeBuilder<BigEmoji> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasDiscriminator<string>("emoji_type")
                .HasValue<RequestedBigEmoji>("requested")
                .HasValue<ApprovedBigEmoji>("approved")
                .HasValue<DeniedBigEmoji>("denied");
        }
    }
}