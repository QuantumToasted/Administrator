using Disqord;

namespace Administrator.Core;

public enum PunishmentType
{
    Ban = 1,
    Block = 2,
    Kick = 3,
    TimedRole = 4,
    Timeout = 5,
    Warning = 6
}

public interface IPunishment
{
    int Id { get; }
    
    Snowflake GuildId { get; }
    
    UserSnapshot Target { get; }
    
    UserSnapshot Moderator { get; }
    
    DateTimeOffset CreatedAt { get; }
    
    string? Reason { get; }
    
    PunishmentType Type { get; }
}