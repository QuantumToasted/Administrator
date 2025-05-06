using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record EmojiStats : IEmojiStats, IEntityTypeConfiguration<EmojiStats>
{
    public Snowflake GuildId { get; init; }
    
    public Snowflake EmojiId { get; init; }
    
    public int Uses { get; set; }
    
    public GuildConfiguration? Guild { get; init; }

    public static EmojiStats Create(IGuildEmoji emoji) => Create(emoji.GuildId, emoji.Id);
    public static EmojiStats Create(Snowflake guildId, Snowflake emojiId)
    {
        return new EmojiStats
        {
            GuildId = guildId,
            EmojiId = emojiId
        };
    }

    void IEntityTypeConfiguration<EmojiStats>.Configure(EntityTypeBuilder<EmojiStats> stats)
    {
        stats.HasKey(x => x.EmojiId);
        stats.HasIndex(x => x.GuildId);
    }
}