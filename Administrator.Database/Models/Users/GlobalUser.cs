using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Administrator.Database;

[Table("global_users")]
[PrimaryKey(nameof(UserId))]
public sealed record GlobalUser(
    ulong UserId) 
    : User(UserId)
{
    [Column("sent_initial_join_message")]
    public bool WasSentInitialJoinMessage { get; set; }
    
    [Column("timezone")]
    public TimeZoneInfo? TimeZone { get; set; }
    
    [Column("highlights_snoozed_until")]
    public DateTimeOffset? HighlightsSnoozedUntil { get; set; }
    
    [Column("blacklisted_highlight_users")]
    public HashSet<ulong> BlacklistedHighlightUserIds { get; init; } = new();

    [Column("blacklisted_highlight_channels")]
    public HashSet<ulong> BlacklistedHighlightChannelIds { get; init; } = new();
    
    public List<Highlight> Highlights { get; init; }
    
    public List<Reminder> Reminders { get; init; }
}