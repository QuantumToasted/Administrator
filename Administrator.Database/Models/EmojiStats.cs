using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record EmojiStats(Snowflake EmojiId, Snowflake GuildId) : IStaticEntityTypeConfiguration<EmojiStats>
{
    public int Uses { get; set; }
    
    public Guild? Guild { get; init; }

    static void IStaticEntityTypeConfiguration<EmojiStats>.ConfigureBuilder(EntityTypeBuilder<EmojiStats> stats)
    {
        stats.ToTable("emoji_stats");
        stats.HasKey(x => x.EmojiId);
        stats.HasIndex(x => x.GuildId);

        stats.HasPropertyWithColumnName(x => x.EmojiId, "emoji");
        stats.HasPropertyWithColumnName(x => x.GuildId, "guild");
        stats.HasPropertyWithColumnName(x => x.Uses, "uses");
    }
}