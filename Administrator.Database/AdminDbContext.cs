using Disqord;
using Microsoft.EntityFrameworkCore;

namespace Administrator.Database;

#pragma warning disable CS8618
public sealed class AdminDbContext(DbContextOptions<AdminDbContext> options) : DbContext(options)
{
    
    public DbSet<Guild> Guilds { get; init; }

    public DbSet<Punishment> Punishments { get; init; }

    public DbSet<WarningPunishment> WarningPunishments { get; init; }

    public DbSet<LoggingChannel> LoggingChannels { get; init; }

    public DbSet<Reminder> Reminders { get; init; }

    public DbSet<Tag> Tags { get; init; }

    public DbSet<Highlight> Highlights { get; init; }

    public DbSet<GlobalUser> GlobalUsers { get; init; }

    public DbSet<GuildUser> GuildUsers { get; init; }

    public DbSet<InviteFilterExemption> InviteFilterExemptions { get; init; }

    public DbSet<EmojiStats> EmojiStats { get; init; }

    public DbSet<RoleLevelReward> LevelRewards { get; init; }

    public DbSet<ButtonRole> ButtonRoles { get; init; }

    public DbSet<LuaCommand> LuaCommands { get; init; }

    public DbSet<ForumAutoTag> AutoTags { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyStaticConfigurationsFromAssembly(GetType().Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<TimeZoneInfo?>()
            .HaveConversion(typeof(TimeZoneInfoConverter));

        configurationBuilder.Properties<HashSet<Snowflake>>()
            .HaveConversion(typeof(SnowflakeHashSetConverter), typeof(HashSetValueComparer<Snowflake>));

        configurationBuilder.Properties<Snowflake>()
            .HaveConversion(typeof(SnowflakeConverter));

        configurationBuilder.Properties<Snowflake?>()
            .HaveConversion(typeof(NullableSnowflakeConverter));
    }
}
#pragma warning restore CS8618