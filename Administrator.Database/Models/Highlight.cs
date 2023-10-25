using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Administrator.Database;

[Table("highlights")]
[PrimaryKey(nameof(Id))]
[Index(nameof(Text))]
public sealed record Highlight(
    [property: Column("author")] ulong AuthorId, 
    [property: Column("guild")] ulong? GuildId, 
    [property: Column("text")] string Text)
{
    [Column("id")]
    public int Id { get; init; }
    
    [ForeignKey(nameof(AuthorId))]
    public GlobalUser? Author { get; init; }
}