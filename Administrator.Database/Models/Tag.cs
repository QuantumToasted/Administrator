using System.ComponentModel.DataAnnotations.Schema;
using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;

namespace Administrator.Database;

[Table("tags")]
[PrimaryKey(nameof(GuildId), nameof(Name))]
[Index(nameof(OwnerId))]
[Index(nameof(Aliases), IsUnique = true)]
public sealed record Tag(
    [property: Column("guild")] Snowflake GuildId,
    Snowflake OwnerId,
    [property: Column("name")] string Name)
{
    [Column("aliases")] 
    public List<string> Aliases { get; set; } = new();
    
    [Column("owner")]
    public Snowflake OwnerId { get; set; } = OwnerId;
    
    [Column("created")]
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    
    [Column("message", TypeName = "jsonb")]
    public JsonMessage? Message { get; set; }
    
    [Column("uses")]
    public int Uses { get; set; }
    
    [Column("last_used")]
    public DateTimeOffset? LastUsedAt { get; set; }
    
    [Column("attachment")]
    public Guid? AttachmentId { get; set; }
    
    [ForeignKey(nameof(AttachmentId))]
    public Attachment? Attachment { get; set; }
    
    [ForeignKey("GuildId,OwnerId")]
    public GuildUser? Owner { get; init; }
    
    [ForeignKey(nameof(GuildId))]
    public Guild? Guild { get; init; }
}