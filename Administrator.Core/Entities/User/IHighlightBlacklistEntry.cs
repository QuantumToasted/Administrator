using Disqord;

namespace Administrator.Core;

public interface IHighlightBlacklistEntry
{
    Snowflake UserId { get; }
    
    Snowflake TargetId { get; }
    
    HighlightBlacklistTargetType TargetType { get; }
    
    string? Reason { get; }
}