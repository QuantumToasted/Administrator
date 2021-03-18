using Disqord;

namespace Administrator.Database
{
    public interface IGuildDbEntity
    {
        Snowflake GuildId { get; set; }
    }
}