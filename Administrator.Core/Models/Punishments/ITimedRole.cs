using Disqord;

namespace Administrator.Core;

public enum TimedRoleApplyMode
{
    Grant = 1,
    Revoke
}

public interface ITimedRole : IRevocablePunishment, IExpiringEntity
{
    Snowflake RoleId { get; }
    
    TimedRoleApplyMode Mode { get; }
    
    PunishmentType IPunishment.Type => PunishmentType.TimedRole;
}