using System.ComponentModel.DataAnnotations.Schema;
using Disqord;
using Microsoft.EntityFrameworkCore;

namespace Administrator.Database;

[Table("highlights")]
[PrimaryKey(nameof(Id))]
[Index(nameof(Text))]
public sealed record Highlight(
    [property: Column("author")] Snowflake AuthorId, 
    [property: Column("guild")] Snowflake? GuildId, 
    [property: Column("text")] string Text) : INumberKeyedDbEntity<int>
{
    [Column("id")]
    public int Id { get; init; }
    
    [ForeignKey(nameof(AuthorId))]
    public GlobalUser? Author { get; init; }
}