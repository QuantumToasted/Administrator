using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public enum ReminderRepeatMode
{
    Hourly,
    Daily,
    Weekly
}

public sealed record Reminder(string Text, Snowflake AuthorId, Snowflake ChannelId, DateTimeOffset ExpiresAt, ReminderRepeatMode? RepeatMode, double? RepeatInterval) 
    : INumberKeyedDbEntity<int>, IExpiringDbEntity
{
    public int Id { get; init; }
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset ExpiresAt { get; set; } = ExpiresAt;
    
    public User? Author { get; init; }

    DateTimeOffset? IExpiringDbEntity.ExpiresAt => ExpiresAt;
    
    public override string ToString()
        => this.FormatKey();

    private sealed class ReminderConfiguration : IEntityTypeConfiguration<Reminder>
    {
        public void Configure(EntityTypeBuilder<Reminder> reminder)
        {
            reminder.HasKey(x => x.Id);
            reminder.HasIndex(x => x.ExpiresAt);
        }
    }
}