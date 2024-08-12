using System.ComponentModel;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public enum LogEventType
{
    [Description("Users being banned.")]
    Ban = 1,
    [Description("Users being kicked.")]
    Kick,
    [Description("Users being timed out.")]
    Timeout,
    [Description("Users being blocked from a channel.")]
    Block,
    [Description("Users being given a timed role or having one taken away.")]
    TimedRole,
    [Description("Users being given a warning.")]
    Warning,
    [Description("Users appealing a punishment.")]
    Appeal,
    [Description("Punishments being revoked.")]
    Revoke,
    [Description("Messages being updated.")]
    MessageUpdate,
    [Description("Messages being deleted.")]
    MessageDelete,
    [Description("Users joining the server.")]
    Join,
    [Description("Users leaving the server.")]
    Leave,
    [Description("Greetings sent for users joining the server.")]
    Greeting,
    [Description("Goodbyes sent for users leaving the server.")]
    Goodbye,
    [Description("Users updating their avatar.")]
    AvatarUpdate,
    [Description("Users updating their nickname or username.")]
    NameUpdate,
    [Description("Users having their roles updated.")]
    UserRoleUpdate,
    [Description("Unhandled bot errors occurring.")]
    Errors,
    [Description("Important bot announcements and changelogs.")]
    BotAnnouncements
}

public sealed record LoggingChannel(Snowflake GuildId, LogEventType EventType, Snowflake ChannelId)
{
    public Snowflake ChannelId { get; set; } = ChannelId;
    
    public Guild? Guild { get; init; }

    private sealed class LoggingChannelConfiguration : IEntityTypeConfiguration<LoggingChannel>
    {
        public void Configure(EntityTypeBuilder<LoggingChannel> channel)
        {
            channel.HasKey(x => new { x.GuildId, x.EventType });
        }
    }
}