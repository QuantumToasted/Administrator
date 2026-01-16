using Disqord;
using NodaTime;

namespace Administrator.Core;

public interface ITag : ITimestampedEntity
{
    string Name { get; }
    
    Snowflake OwnerId { get; }
    
    Snowflake GuildId { get; }
    
    JsonMessage? Message { get; }
    
    int Uses { get; }
    
    Instant? LastUsedAt { get; }
    
    Guid? AttachmentId { get; } // TODO: Implement attachments
    
    /*
     *     Snowflake OwnerId { get; }
    
    string Name { get; }
    
    string[] Aliases { get; }
    
    DateTimeOffset CreatedAt { get; }
    
    JsonMessage? Message { get;}
    
    int Uses { get; }
    
    DateTimeOffset? LastUsedAt { get; }
    
    Guid? AttachmentId { get; }
     */
}