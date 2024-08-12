using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record ForumAutoTag(Snowflake ChannelId, Snowflake GuildId, string Text, bool IsRegex, Snowflake TagId)
{
    public int Id { get; init; }
    
    public Guild? Guild { get; init; }

    private sealed class ForumAutoTagConfiguration : IEntityTypeConfiguration<ForumAutoTag>
    {
        public void Configure(EntityTypeBuilder<ForumAutoTag> autoTag)
        {
            autoTag.HasKey(x => x.Id);
            autoTag.HasIndex(x => x.ChannelId);
        }
    }
}