using Disqord;

namespace Administrator.Core;

public interface IEmojiStats : IGuildEntity
{
    public Snowflake EmojiId { get; }
    
    public int Uses { get; }
}