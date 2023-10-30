using System.ComponentModel.DataAnnotations.Schema;
using Disqord;
using Microsoft.EntityFrameworkCore;

namespace Administrator.Database;

[Table("emoji_stats")]
[PrimaryKey(nameof(EmojiId))]
public sealed record EmojiStats(
    [property: Column("emoji")] Snowflake EmojiId,
    [property: Column("guild")] Snowflake GuildId)
{
    [Column("uses")]
    public int Uses { get; set; }
    
    [ForeignKey(nameof(GuildId))]
    public Guild? Guild { get; init; }
}