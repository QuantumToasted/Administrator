using Disqord;

namespace Administrator.Database
{
    public interface IUserDbEntity
    {
        Snowflake UserId { get; set; }
    }
}