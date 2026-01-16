using Disqord;

namespace Administrator.Core;

public interface IPermissionsPlus
{
    Snowflake GuildId { get; }
    
    Snowflake TargetId { get; }
    
    PermissionsTargetType TargetType { get; }
    
    PermissionsPlus Permissions { get; }
}