using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record EmojiStats(Snowflake EmojiId, Snowflake GuildId)
{
    public int Uses { get; set; }
    
    public Guild? Guild { get; init; }

    private sealed class EmojiStatsConfiguration : IEntityTypeConfiguration<EmojiStats>
    {
        public void Configure(EntityTypeBuilder<EmojiStats> stats)
        {
            stats.HasKey(x => x.EmojiId);
            stats.HasIndex(x => x.GuildId);
        }
    }
}