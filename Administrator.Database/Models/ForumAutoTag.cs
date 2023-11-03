using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record ForumAutoTag(Snowflake ChannelId, Snowflake GuildId, string Text, bool IsRegex, Snowflake TagId) 
    : IStaticEntityTypeConfiguration<ForumAutoTag>
{
    public int Id { get; init; }
    
    public Guild? Guild { get; init; }

    static void IStaticEntityTypeConfiguration<ForumAutoTag>.ConfigureBuilder(EntityTypeBuilder<ForumAutoTag> autoTag)
    {
        autoTag.ToTable("forum_auto_tags");
        autoTag.HasKey(x => x.Id);
        autoTag.HasIndex(x => x.ChannelId);

        autoTag.HasPropertyWithColumnName(x => x.ChannelId, "channel");
        autoTag.HasPropertyWithColumnName(x => x.GuildId, "guild");
        autoTag.HasPropertyWithColumnName(x => x.Text, "text");
        autoTag.HasPropertyWithColumnName(x => x.IsRegex, "is_regex");
        autoTag.HasPropertyWithColumnName(x => x.TagId, "tag");
    }
}