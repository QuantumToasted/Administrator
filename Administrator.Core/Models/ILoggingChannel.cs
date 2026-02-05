using System.ComponentModel;
using Disqord;

namespace Administrator.Core;

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

public interface ILoggingChannel : IGuildEntity, IChannelEntity
{
    LogEventType EventType { get; }
}