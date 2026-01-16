using Disqord;

namespace Administrator.Core;

public interface IUserXp
{
    Snowflake UserId { get; }
    
    Snowflake? GuildId { get; }
    
    int TotalXp { get; set; }
    
    DateTimeOffset LastXpGain { get; set; }
    
    DateTimeOffset LastLevelUp { get; set; }
}