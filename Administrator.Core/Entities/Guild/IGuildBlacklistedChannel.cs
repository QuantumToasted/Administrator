using Disqord;

namespace Administrator.Core;

public interface IGuildBlacklistedChannel
{
    Snowflake GuildId { get; }
    
    Snowflake ChannelId { get; }
    
    
}