using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record User(Snowflake UserId) : UserBase(UserId)
{
    public bool WasSentInitialJoinMessage { get; set; }
    
    public TimeZoneInfo? TimeZone { get; set; }
    
    public DateTimeOffset? HighlightsSnoozedUntil { get; set; }

    public List<Snowflake> BlacklistedHighlightUserIds { get; init; } = new();

    public List<Snowflake> BlacklistedHighlightChannelIds { get; init; } = new();

    public int ResumeHighlightsAfterMessageCount { get; set; } = 25;
    
    public TimeSpan ResumeHighlightsAfterInterval { get; set; } = TimeSpan.FromMinutes(10);
    
#pragma warning disable CS8618
    public List<Highlight> Highlights { get; init; }
    
    public List<Reminder> Reminders { get; init; }
#pragma warning restore CS8618

    private sealed class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> user)
        {
            user.HasKey(x => x.UserId);

            user.HasMany(x => x.Highlights).WithOne(x => x.Author).HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Cascade).OnDelete(DeleteBehavior.NoAction);
            user.HasMany(x => x.Reminders).WithOne(x => x.Author).HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Cascade).OnDelete(DeleteBehavior.NoAction);
        }
    }
}