using Disqord;

namespace Administrator.Core;

public interface IForumAutoTag : IKeyedEntity<int>, IGuildEntity, IChannelEntity
{
    string Text { get; }
    
    bool IsRegex { get; }
    
    Snowflake TagId { get; }
}