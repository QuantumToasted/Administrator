using System.ComponentModel.DataAnnotations.Schema;
using Disqord;
using Microsoft.EntityFrameworkCore;

namespace Administrator.Database;

[Table("forum_auto_tags")]
[PrimaryKey(nameof(Id))]
[Index(nameof(ChannelId))]
public sealed record ForumAutoTag(
    [property: Column("channel")] Snowflake ChannelId,
    [property: Column("guild")] Snowflake GuildId,
    [property: Column("text")] string Text,
    [property: Column("regex")] bool IsRegex,
    [property: Column("tag")] Snowflake TagId)
{
    [Column("id")] 
    public int Id { get; init; }
    
    [ForeignKey(nameof(GuildId))]
    public Guild? Guild { get; init; }
}