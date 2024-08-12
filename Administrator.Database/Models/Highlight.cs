using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record Highlight(Snowflake AuthorId, Snowflake? GuildId, string Text) : INumberKeyedDbEntity<int>
{
    public int Id { get; init; }
    
    public User? Author { get; init; }

    public override string ToString()
        => this.FormatKey();

    private sealed class HighlightConfiguration : IEntityTypeConfiguration<Highlight>
    {
        public void Configure(EntityTypeBuilder<Highlight> highlight)
        {
            highlight.HasKey(x => x.Id);
        }
    }
}