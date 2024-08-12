using Disqord;

namespace Administrator.Core;

public interface IBlock : IRevocablePunishment
{
    Snowflake ChannelId { get; }
    
    DateTimeOffset? ExpiresAt { get; }
}