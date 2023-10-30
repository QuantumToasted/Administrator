using System.ComponentModel.DataAnnotations.Schema;
using Disqord;
using Microsoft.EntityFrameworkCore;

namespace Administrator.Database;

[Table("global_users")]
[PrimaryKey(nameof(UserId))]
public sealed record GlobalUser(
    Snowflake UserId) 
    : User(UserId)
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
    
    public List<Highlight>? Highlights { get; init; }
    
    public List<Reminder>? Reminders { get; init; }
}