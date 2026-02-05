using Disqord;

namespace Administrator.Core;

public interface ITag : IGuildEntity
{
    Snowflake OwnerId { get; }
    
    string Name { get; }
    
    string[] Aliases { get; }
    
    DateTimeOffset CreatedAt { get; }
    
    JsonMessage? Message { get;}
    
    int Uses { get; }
    
    DateTimeOffset? LastUsedAt { get; }
    
    Guid? AttachmentId { get; }
}