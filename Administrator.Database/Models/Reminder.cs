using System.ComponentModel.DataAnnotations.Schema;
using Disqord;
using Microsoft.EntityFrameworkCore;

namespace Administrator.Database;

public enum ReminderRepeatMode
{
    Hourly,
    Daily,
    Weekly
}

[Table("reminders")]
[PrimaryKey(nameof(Id))]
[Index(nameof(ExpiresAt))]
public sealed record Reminder(
    [property: Column("text")] string Text, 
    [property: Column("author")] Snowflake AuthorId, 
    [property: Column("channel")] Snowflake ChannelId,
    DateTimeOffset ExpiresAt,
    //[property: Column("timezone")] TimeZoneInfo AuthorTimeZone, 
    [property: Column("mode")] ReminderRepeatMode? RepeatMode, 
    [property: Column("interval")] double? RepeatInterval) : INumberKeyedDbEntity<int>, IExpiringDbEntity
{
    [Column("id")] 
    public int Id { get; init; }
    
    [Column("created")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("expires")] 
    public DateTimeOffset ExpiresAt { get; set; } = ExpiresAt;
    
    [ForeignKey(nameof(AuthorId))]
    public GlobalUser? Author { get; init; }

    DateTimeOffset? IExpiringDbEntity.ExpiresAt => ExpiresAt;
}