using Disqord;

namespace Administrator.Core;

public interface IButtonRole : IKeyedEntity<int>, IGuildEntity, IChannelEntity
{
    Snowflake MessageId { get; }
    
    int Row { get; }
    
    int Position { get; }
    
    IEmoji? Emoji { get; }
    
    string? Text { get; }
    
    LocalButtonComponentStyle Style { get; }
    
    Snowflake RoleId { get; }
    
    int? ExclusiveGroupId { get; }
}