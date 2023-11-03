using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record Tag(Snowflake GuildId, Snowflake OwnerId, string Name) : IStaticEntityTypeConfiguration<Tag>
{
    private string[] _aliases = Array.Empty<string>();

    public IReadOnlyList<string> Aliases
    {
        get => _aliases.ToList();
        set => _aliases = value.ToArray();
    }
    
    public Snowflake OwnerId { get; set; } = OwnerId;
    
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    
    public JsonMessage? Message { get; set; }
    
    public int Uses { get; set; }
    
    public DateTimeOffset? LastUsedAt { get; set; }
    
    public Guid? AttachmentId { get; set; }
    
    public Attachment? Attachment { get; set; }
    
    public GuildUser? Owner { get; init; }
    
    public Guild? Guild { get; init; }

    static void IStaticEntityTypeConfiguration<Tag>.ConfigureBuilder(EntityTypeBuilder<Tag> tag)
    {
        tag.ToTable("tags");
        tag.HasKey(x => new { x.GuildId, x.Name });
        tag.HasIndex(x => x.OwnerId);
        tag.HasIndex(x => x._aliases).IsUnique();

        tag.HasPropertyWithColumnName(x => x.GuildId, "guild");
        tag.HasPropertyWithColumnName(x => x.OwnerId, "owner");
        tag.HasPropertyWithColumnName(x => x.Name, "name");
        tag.HasPropertyWithColumnName(x => x._aliases, "aliases");
        tag.Ignore(x => x.Aliases);
        tag.HasPropertyWithColumnName(x => x.CreatedAt, "created");
        tag.HasPropertyWithColumnName(x => x.Message, "message").HasColumnType("jsonb");
        tag.HasPropertyWithColumnName(x => x.Uses, "uses");
        tag.HasPropertyWithColumnName(x => x.LastUsedAt, "last_used");
        tag.HasPropertyWithColumnName(x => x.AttachmentId, "attachment");
    }
}