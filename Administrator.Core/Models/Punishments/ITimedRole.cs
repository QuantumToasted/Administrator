using Disqord;

namespace Administrator.Core;

public enum TimedRoleApplyMode
{
    Grant = 1,
    Revoke
}

public interface ITimedRole : IRevocablePunishment
{
    Snowflake RoleId { get; }
    
    TimedRoleApplyMode Mode { get; }
    
    DateTimeOffset? ExpiresAt { get; }
    
    PunishmentType IPunishment.Type => PunishmentType.TimedRole;
}