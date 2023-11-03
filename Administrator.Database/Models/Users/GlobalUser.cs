using System.ComponentModel.DataAnnotations.Schema;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

[Table("global_users")]
[PrimaryKey(nameof(UserId))]
public sealed record GlobalUser(Snowflake UserId) : User(UserId), IStaticEntityTypeConfiguration<GlobalUser>
{
    [Column("sent_initial_join_message")]
    public bool WasSentInitialJoinMessage { get; set; }
    
    [Column("timezone")]
    public TimeZoneInfo? TimeZone { get; set; }
    
    [Column("highlights_snoozed_until")]
    public DateTimeOffset? HighlightsSnoozedUntil { get; set; }
    
    [Column("blacklisted_highlight_users")]
    public HashSet<Snowflake> BlacklistedHighlightUserIds { get; init; } = new();

    [Column("blacklisted_highlight_channels")]
    public HashSet<Snowflake> BlacklistedHighlightChannelIds { get; init; } = new();

    [Column("resume_highlights_count")]
    public int ResumeHighlightsAfterMessageCount { get; set; } = 25;
    
    [Column("resume_highlights_interval")]
    public TimeSpan ResumeHighlightsAfterInterval { get; set; } = TimeSpan.FromMinutes(10);
    
#pragma warning disable CS8618
    public List<Highlight> Highlights { get; init; }
    
    public List<Reminder> Reminders { get; init; }
#pragma warning restore CS8618
    
    static void IStaticEntityTypeConfiguration<GlobalUser>.ConfigureBuilder(EntityTypeBuilder<GlobalUser> user)
    {
        user.ToTable("global_users");
        user.HasKey(x => x.UserId);

        user.HasPropertyWithColumnName(x => x.UserId, "user");
        user.HasPropertyWithColumnName(x => x.WasSentInitialJoinMessage, "sent_initial_join_message");
        user.HasPropertyWithColumnName(x => x.TimeZone, "timezone");
        user.HasPropertyWithColumnName(x => x.HighlightsSnoozedUntil, "highlights_snoozed_until");
        user.HasPropertyWithColumnName(x => x.BlacklistedHighlightUserIds, "highlights_user_blacklist");
        user.HasPropertyWithColumnName(x => x.BlacklistedHighlightChannelIds, "highlights_channel_blacklist");
        user.HasPropertyWithColumnName(x => x.ResumeHighlightsAfterMessageCount, "highlights_resume_count");
        user.HasPropertyWithColumnName(x => x.ResumeHighlightsAfterInterval, "highlights_resume_interval");

        user.HasMany(x => x.Highlights).WithOne(x => x.Author).HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Cascade);
        user.HasMany(x => x.Reminders).WithOne(x => x.Author).HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Cascade);
        // user.HasMany(x => x.Punishments).WithOne(x => x.TargetUser).HasForeignKey(x => x.Target.Id).OnDelete(DeleteBehavior.NoAction); no jsonb foreign keys :/
    }
}