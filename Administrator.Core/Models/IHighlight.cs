using Disqord;

namespace Administrator.Core;

public interface IHighlight : IKeyedEntity<int>, IPossiblyGuildEntity
{
    Snowflake AuthorId { get; }
    
    string Text { get; }
}