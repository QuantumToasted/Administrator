using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public enum LogEventType
{
    //[ChoiceName("Errors - Unhandled bot errors occurring.")]
    Errors = 1,
    //[ChoiceName("BotAnnouncements - Important bot announcements and changelogs.")]
    BotAnnouncements,
    //[ChoiceName("Ban - Users being banned.")]
    Ban,
    //[ChoiceName("Kick - Users being kicked.")]
    Kick,
    //[ChoiceName("Timeout - Users being timed out.")]
    Timeout,
    //[ChoiceName("Block - Users being blocked from a channel..")]
    Block,
    //[ChoiceName("TimedRole - Users being given a timed role or having one taken away.")]
    TimedRole,
    //[ChoiceName("Warning - Users being given a warning.")]
    Warning,
    //[ChoiceName("Appeal - Users appealing a punishment.")]
    Appeal,
    //[ChoiceName("Revoke - Punishments being revoked.")]
    Revoke,
    //[ChoiceName("MessageUpdate - Messages being updated.")]
    MessageUpdate,
    //[ChoiceName("MessageDelete - Messages being deleted.")]
    MessageDelete,
    //[ChoiceName("Join - Users joining the server.")]
    Join,
    //[ChoiceName("Leave - Users leaving the server.")]
    Leave,
    //[ChoiceName("Greeting - Greetings sent for users joining the server.")]
    Greeting,
    //[ChoiceName("Goodbye - Goodbyes sent for users leaving the server.")]
    Goodbye,
    //[ChoiceName("AvatarUpdate - Users updating their avatar.")]
    AvatarUpdate,
    //[ChoiceName("NameUpdate - Users updating their nickname or username.")]
    NameUpdate,
    //[ChoiceName("UserRoleUpdate - Users having their roles updated.")]
    UserRoleUpdate
}

public sealed record LoggingChannel(Snowflake GuildId, LogEventType EventType, Snowflake ChannelId) : IStaticEntityTypeConfiguration<LoggingChannel>
{
    public Snowflake ChannelId { get; set; } = ChannelId;
    
    public Guild? Guild { get; init; }

    static void IStaticEntityTypeConfiguration<LoggingChannel>.ConfigureBuilder(EntityTypeBuilder<LoggingChannel> channel)
    {
        channel.ToTable("logging_channels");
        channel.HasKey(x => new { x.GuildId, x.EventType });

        channel.HasPropertyWithColumnName(x => x.GuildId, "guild");
        channel.HasPropertyWithColumnName(x => x.EventType, "type");
        channel.HasPropertyWithColumnName(x => x.ChannelId, "channel");
    }
}