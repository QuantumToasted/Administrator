using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Administrator.Database;

[Table("emoji_stats")]
[PrimaryKey(nameof(EmojiId))]
public sealed record EmojiStats(
    [property: Column("emoji")] ulong EmojiId,
    [property: Column("guild")] ulong GuildId)
{
    [Column("uses")]
    public int Uses { get; set; }
    
    [ForeignKey(nameof(GuildId))]
    public Guild? Guild { get; init; }
}