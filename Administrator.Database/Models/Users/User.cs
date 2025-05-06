using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed class User : IUserXp, IUserConfiguration, IEntityTypeConfiguration<User>
{
    public Snowflake UserId { get; init; }

    public int TotalXp { get; set; }
    
    public DateTimeOffset LastXpGain { get; set; } = DateTimeOffset.UtcNow;
    
    public DateTimeOffset LastLevelUp { get; set; } = DateTimeOffset.UtcNow;
    
    public bool WasSentInitialJoinMessage { get; set; }
    
    public TimeZoneInfo? TimeZone { get; set; }
    
    public DateTimeOffset? HighlightsSnoozedUntil { get; set; }

    public List<Snowflake> BlacklistedHighlightUserIds { get; init; } = new();

    public List<Snowflake> BlacklistedHighlightChannelIds { get; init; } = new();

    public int ResumeHighlightsAfterMessageCount { get; init; } = 25;
    
    public TimeSpan ResumeHighlightsAfterInterval { get; init; } = TimeSpan.FromMinutes(10);
    
#pragma warning disable CS8618
    public List<Highlight> Highlights { get; init; }
    
    public List<Reminder> Reminders { get; init; }
#pragma warning restore CS8618

    public static User Create(IUser user) => Create(user.Id);
    public static User Create(Snowflake userId)
    {
        return new User
        {
            UserId = userId
        };
    }
    
    IReadOnlyList<Snowflake> IUserConfiguration.BlacklistedHighlightUserIds => BlacklistedHighlightUserIds;
    IReadOnlyList<Snowflake> IUserConfiguration.BlacklistedHighlightChannelIds => BlacklistedHighlightChannelIds;
    void IEntityTypeConfiguration<User>.Configure(EntityTypeBuilder<User> user)
    {
        user.HasKey(x => x.UserId);

        user.HasMany(x => x.Highlights).WithOne(x => x.Author).HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.NoAction);
        user.HasMany(x => x.Reminders).WithOne(x => x.Author).HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.NoAction);
    }
}