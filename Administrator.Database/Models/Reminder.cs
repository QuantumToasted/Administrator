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
    : INumberKeyedDbEntity<int>, IExpiringDbEntity, IStaticEntityTypeConfiguration<Reminder>
{
    public int Id { get; init; }
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset ExpiresAt { get; set; } = ExpiresAt;
    
    public GlobalUser? Author { get; init; }

    DateTimeOffset? IExpiringDbEntity.ExpiresAt => ExpiresAt;
    static void IStaticEntityTypeConfiguration<Reminder>.ConfigureBuilder(EntityTypeBuilder<Reminder> reminder)
    {
        reminder.ToTable("reminders");
        reminder.HasKey(x => x.Id);
        reminder.HasIndex(x => x.ExpiresAt);

        reminder.HasPropertyWithColumnName(x => x.Id, "id");
        reminder.HasPropertyWithColumnName(x => x.CreatedAt, "created");
        reminder.HasPropertyWithColumnName(x => x.Text, "text");
        reminder.HasPropertyWithColumnName(x => x.AuthorId, "author");
        reminder.HasPropertyWithColumnName(x => x.ChannelId, "channel");
        reminder.HasPropertyWithColumnName(x => x.ExpiresAt, "expires");
        reminder.HasPropertyWithColumnName(x => x.RepeatMode, "repeat_mode");
        reminder.HasPropertyWithColumnName(x => x.RepeatInterval, "repeat_interval");
    }
}