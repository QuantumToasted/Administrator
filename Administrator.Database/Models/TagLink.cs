using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record TagLink(Snowflake GuildId, string From, string To, string? Label, LocalButtonComponentStyle Style, bool IsEphemeral)
    : INumberKeyedDbEntity<int>
{
    public int Id { get; init; }
    
    private sealed class TagLinkConfiguration : IEntityTypeConfiguration<TagLink>
    {
        public void Configure(EntityTypeBuilder<TagLink> tagLink)
        {
            tagLink.HasKey(x => new { x.From, x.To });
            tagLink.HasIndex(x => x.From).IsUnique(false);
        }
    }
}