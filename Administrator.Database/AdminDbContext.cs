using Disqord;
using Microsoft.EntityFrameworkCore;

namespace Administrator.Database;

#pragma warning disable CS8618
public sealed class AdminDbContext(DbContextOptions<AdminDbContext> options) : DbContext(options)
{
    public DbSet<Guild> Guilds { get; init; }

    public DbSet<Punishment> Punishments { get; init; }

    public DbSet<AutomaticPunishment> AutomaticPunishments { get; init; }

    public DbSet<LoggingChannel> LoggingChannels { get; init; }

    public DbSet<Reminder> Reminders { get; init; }

    public DbSet<Tag> Tags { get; init; }

    public DbSet<Highlight> Highlights { get; init; }

    public DbSet<User> Users { get; init; }

    public DbSet<Member> Members { get; init; }

    public DbSet<InviteFilterExemption> InviteFilterExemptions { get; init; }

    public DbSet<EmojiStats> EmojiStats { get; init; }

    public DbSet<RoleLevelReward> LevelRewards { get; init; }

    public DbSet<ButtonRole> ButtonRoles { get; init; }

    public DbSet<LuaCommand> LuaCommands { get; init; }

    public DbSet<ForumAutoTag> AutoTags { get; init; }
    
    public DbSet<TagLink> LinkedTags { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<TimeZoneInfo?>()
            .HaveConversion(typeof(TimeZoneInfoConverter));

        configurationBuilder.Properties<List<Snowflake>>()
            .HaveConversion(typeof(SnowflakeListConverter), typeof(ListValueComparer<Snowflake>));

        configurationBuilder.Properties<Snowflake>()
            .HaveConversion(typeof(SnowflakeConverter));

        configurationBuilder.Properties<Snowflake?>()
            .HaveConversion(typeof(NullableSnowflakeConverter));
    }
}
#pragma warning restore CS8618