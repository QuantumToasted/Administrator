using System.ComponentModel.DataAnnotations.Schema;
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

[Table("logging_channels")]
[PrimaryKey(nameof(GuildId), nameof(EventType))]
public sealed record LoggingChannel(
    [property: Column("guild")] ulong GuildId, 
    [property: Column("type")] LogEventType EventType,
    ulong ChannelId)
{
    [Column("channel")] 
    public ulong ChannelId { get; set; } = ChannelId;
    
    [ForeignKey(nameof(GuildId))]
    public Guild? Guild { get; init; }
}