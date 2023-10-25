using System.ComponentModel.DataAnnotations.Schema;
using Administrator.Core;
using Microsoft.EntityFrameworkCore;

namespace Administrator.Database;

[Table("tags")]
[PrimaryKey(nameof(GuildId), nameof(Name))]
[Index(nameof(OwnerId))]
public sealed record Tag(
    [property: Column("guild")] ulong GuildId,
    ulong OwnerId,
    [property: Column("name")] string Name) 
{
    [Column("owner")]
    public ulong OwnerId { get; set; } = OwnerId;
    
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