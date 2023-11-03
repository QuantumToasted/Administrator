using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record Highlight(Snowflake AuthorId, Snowflake? GuildId, string Text) : INumberKeyedDbEntity<int>, 
    IStaticEntityTypeConfiguration<Highlight>
{
    public int Id { get; init; }
    
    public GlobalUser? Author { get; init; }

    static void IStaticEntityTypeConfiguration<Highlight>.ConfigureBuilder(EntityTypeBuilder<Highlight> highlight)
    {
        highlight.ToTable("highlights");
        highlight.HasKey(x => x.Id);

        highlight.HasPropertyWithColumnName(x => x.Id, "id");
        highlight.HasPropertyWithColumnName(x => x.AuthorId, "author");
        highlight.HasPropertyWithColumnName(x => x.GuildId, "guild");
        highlight.HasPropertyWithColumnName(x => x.Text, "text");
    }
}