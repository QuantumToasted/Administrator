using Disqord;

namespace Administrator.Core;

public interface IRoleLevelReward : IGuildEntity
{
    int Tier { get; }
    
    int Level { get; }
    
    IReadOnlyList<Snowflake> GrantedRoleIds { get; }
    
    IReadOnlyList<Snowflake> RevokedRoleIds { get; }
}