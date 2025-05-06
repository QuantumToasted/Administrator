using Disqord;

namespace Administrator.Core;

public enum Grade
{
    Civilian,
    Freelance,
    Mercenary,
    Commando,
    Assassin,
    Elite
}

public interface IUserXp
{
    Snowflake UserId { get; }
    
    int TotalXp { get; set; }
    
    DateTimeOffset LastXpGain { get; set; }
    
    DateTimeOffset LastLevelUp { get; set; }
}