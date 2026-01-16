using Disqord;

namespace Administrator.Core;

public interface IGuildConfiguration
{
    Snowflake GuildId { get; }
}