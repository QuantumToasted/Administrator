using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodaTime;

namespace Administrator.Database;

public sealed record Reminder : IReminder, IEntityTypeConfiguration<Reminder>
{
    public int Id { get; init; }
    
    public Snowflake ChannelId { get; init; }
    
    public DateTimeOffset ExpiresAt { get; set; }

    public string Text { get; init; } = null!;
    
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    
    public Snowflake AuthorId { get; init; }
    
    public ReminderRepeatMode? RepeatMode { get; init; }
    
    public int? RepeatInterval { get; init; }
    
    public User? Author { get; init; }
    
    public override string ToString()
        => this.FormatKey();

    public static Reminder CreateSingle(string text, Snowflake authorId, Snowflake channelId, DateTimeOffset expiresAt)
    {
        return new Reminder
        {
            Text = text,
            AuthorId = authorId,
            ChannelId = channelId,
            ExpiresAt = expiresAt
        };
    }
    
    public static Reminder CreateRepeating(string text, Snowflake authorId, Snowflake channelId, DateTimeOffset? remindAt, ReminderRepeatMode mode, int interval)
    {
        var now = LocalDateTime.FromDateTime(DateTime.UtcNow);
        var expiresAt = remindAt is not null ? LocalDateTime.FromDateTime(remindAt.Value.UtcDateTime) : now;

        while (expiresAt <= now)
        {
            expiresAt = mode switch
            {
                ReminderRepeatMode.Daily => expiresAt.PlusDays(interval),
                ReminderRepeatMode.Weekly => expiresAt.PlusWeeks(interval),
                ReminderRepeatMode.Monthly => expiresAt.PlusMonths(interval),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        
        return new Reminder
        {
            Text = text,
            AuthorId = authorId,
            ChannelId = channelId,
            ExpiresAt = new ZonedDateTime(expiresAt, DateTimeZone.Utc, Offset.Zero).ToDateTimeOffset(),
            RepeatMode = mode,
            RepeatInterval = interval
        };
    }

    DateTimeOffset? IExpiringEntity.ExpiresAt => ExpiresAt;
    void IEntityTypeConfiguration<Reminder>.Configure(EntityTypeBuilder<Reminder> reminder)
    {
        reminder.HasKey(x => x.Id);
        reminder.Property(x => x.Text).HasMaxLength(Discord.Limits.Message.Embed.Field.MaxValueLength);
        //reminder.HasIndex(x => x.ExpiresAt).IsUnique(false); we never use this index
    }
}