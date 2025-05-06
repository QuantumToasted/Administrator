using System.Text.RegularExpressions;
using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed class ForumAutoTag : IForumAutoTag, IEntityTypeConfiguration<ForumAutoTag>
{
    public const int MAX_TEXT_LENGTH = 50;
    
    public int Id { get; init; }
    
    public Snowflake GuildId { get; init; }
    
    public Snowflake ChannelId { get; init; }

    public string Text { get; init; } = null!;
    
    public bool IsRegex { get; init; }
    
    public Snowflake TagId { get; init; }
    
    public GuildConfiguration? Guild { get; init; }
    
    public override string ToString()
        => this.FormatKey();

    public static ForumAutoTag FromText(IForumChannel forum, string text, IForumTag tag) => FromText(forum.GuildId, forum.Id, text, tag.Id);
    public static ForumAutoTag FromText(Snowflake guildId, Snowflake forumId, string text, Snowflake tagId)
    {
        return new ForumAutoTag
        {
            GuildId = guildId,
            ChannelId = forumId,
            Text = text,
            TagId = tagId
        };
    }

    public static ForumAutoTag FromRegex(IForumChannel forum, Regex regex, IForumTag tag) => FromRegex(forum.GuildId, forum.Id, regex, tag.Id);
    public static ForumAutoTag FromRegex(Snowflake guildId, Snowflake forumId, Regex regex, Snowflake tagId)
    {
        return new ForumAutoTag
        {
            GuildId = guildId,
            ChannelId = forumId,
            Text = regex.ToString(),
            IsRegex = true,
            TagId = tagId
        };
    }

    void IEntityTypeConfiguration<ForumAutoTag>.Configure(EntityTypeBuilder<ForumAutoTag> autoTag)
    {
        autoTag.HasKey(x => x.Id);
        autoTag.HasIndex(x => x.ChannelId);
        autoTag.Property(x => x.Text).HasMaxLength(MAX_TEXT_LENGTH);
    }
}