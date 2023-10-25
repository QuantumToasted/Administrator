using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Administrator.Database;

[Table("forum_auto_tags")]
[PrimaryKey(nameof(Id))]
[Index(nameof(Text), IsUnique = true)]
public sealed record ForumAutoTag(
    [property: Column("channel")] ulong ChannelId,
    [property: Column("guild")] ulong GuildId,
    [property: Column("text")] string Text,
    [property: Column("regex")] bool IsRegex,
    [property: Column("tag")] ulong TagId)
{
    [Column("id")] 
    public int Id { get; init; }
    
    [ForeignKey(nameof(GuildId))]
    public Guild? Guild { get; init; }
}