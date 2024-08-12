using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record Tag(Snowflake GuildId, Snowflake OwnerId, string Name)
{
    public string[] Aliases { get; set; } = [];
    
    public Snowflake OwnerId { get; set; } = OwnerId;
    
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    
    public JsonMessage? Message { get; set; }
    
    public int Uses { get; set; }
    
    public DateTimeOffset? LastUsedAt { get; set; }
    
    public Guid? AttachmentId { get; set; }
    
    public Attachment? Attachment { get; set; }
    
    public Member? Owner { get; init; }
    
    public Guild? Guild { get; init; }

    public override string ToString()
        => Name;

    private sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
    {
        public void Configure(EntityTypeBuilder<Tag> tag)
        {
            tag.HasKey(x => new { x.GuildId, x.Name });
            tag.HasIndex(x => x.OwnerId);
            tag.HasIndex(x => x.Aliases).IsUnique(false);

            tag.Property(x => x.Message).HasColumnType("jsonb");
        }
    }
}