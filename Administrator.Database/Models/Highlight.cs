using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed class Highlight : IHighlight, IEntityTypeConfiguration<Highlight>
{
    public const int MAX_HIGHLIGHT_LENGTH = 50;
    
    public int Id { get; init; }
    
    public Snowflake? GuildId { get; init; }
    
    public Snowflake AuthorId { get; init; }

    public string Text { get; init; } = null!;
    
    public User? Author { get; init; }
    
    public override string ToString()
        => this.FormatKey();

    public static Highlight Create(Snowflake? guildId, Snowflake authorId, string text)
    {
        return new Highlight
        {
            GuildId = guildId,
            AuthorId = authorId,
            Text = text
        };
    }
    
    void IEntityTypeConfiguration<Highlight>.Configure(EntityTypeBuilder<Highlight> highlight)
    {
        highlight.HasKey(x => x.Id);
        highlight.Property(x => x.Text).HasMaxLength(MAX_HIGHLIGHT_LENGTH);
    }
}