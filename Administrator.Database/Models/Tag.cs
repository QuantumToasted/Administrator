using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record Tag : ITag, IEntityTypeConfiguration<Tag>
{
    public Snowflake GuildId { get; init; }
    
    public Snowflake OwnerId { get; set; }

    public string Name { get; init; } = null!;

    public string[] Aliases { get; set; } = [];
    
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    
    public JsonMessage? Message { get; set; }
    
    public int Uses { get; set; }
    
    public DateTimeOffset? LastUsedAt { get; set; }
    
    public Guid? AttachmentId { get; init; }
    
    public RemoteAttachment? Attachment { get; set; }
    
    public GuildConfiguration? Guild { get; init; }
    
    public Member? Owner { get; init; }
    
    public override string ToString() 
        => Name;

    public static Tag Create(IMember member, string name) => Create(member.GuildId, member.Id, name);
    public static Tag Create(Snowflake guildId, Snowflake ownerId, string name)
    {
        return new Tag
        {
            GuildId = guildId,
            OwnerId = ownerId,
            Name = name
        };
    }
    
    void IEntityTypeConfiguration<Tag>.Configure(EntityTypeBuilder<Tag> tag)
    {
        tag.HasKey(x => new { x.GuildId, x.Name });
        tag.Property(x => x.Name).HasMaxLength(Discord.Limits.Component.Button.MaxLabelLength);
        tag.HasIndex(x => x.OwnerId);
        tag.HasIndex(x => x.Aliases).IsUnique(false);

        tag.Property(x => x.Message).HasColumnType("jsonb");
    }
}