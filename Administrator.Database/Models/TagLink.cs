using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed class TagLink : ITagLink, IEntityTypeConfiguration<TagLink>
{
    public int Id { get; init; }
    
    public Snowflake GuildId { get; init; }

    public string From { get; init; } = null!;

    public string To { get; init; } = null!;
    
    public string? Label { get; init; }
    
    public LocalButtonComponentStyle Style { get; init; }
    
    public bool IsEphemeral { get; init; }

    public override string ToString()
        => this.FormatKey();

    public static TagLink Create(Snowflake guildId, string from, string to, string? label, LocalButtonComponentStyle style, bool isEphemeral)
    {
        return new TagLink
        {
            GuildId = guildId,
            From = from,
            To = to,
            Label = label,
            Style = style,
            IsEphemeral = isEphemeral
        };
    }

    void IEntityTypeConfiguration<TagLink>.Configure(EntityTypeBuilder<TagLink> tagLink)
    {
        tagLink.HasKey(x => new { x.From, x.To });
        tagLink.HasIndex(x => x.From).IsUnique(false);
        tagLink.Property(x => x.From).HasMaxLength(Discord.Limits.Component.Button.MaxLabelLength);
        tagLink.Property(x => x.To).HasMaxLength(Discord.Limits.Component.Button.MaxLabelLength);
        tagLink.Property(x => x.Label).HasMaxLength(Discord.Limits.Component.Button.MaxLabelLength);
    }
}