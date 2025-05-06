using Disqord;

namespace Administrator.Core;

public interface IUserConfiguration
{
    TimeZoneInfo? TimeZone { get; }
    
    DateTimeOffset? HighlightsSnoozedUntil { get; }
    
    IReadOnlyList<Snowflake> BlacklistedHighlightUserIds { get; }
    
    IReadOnlyList<Snowflake> BlacklistedHighlightChannelIds { get; }
    
    int ResumeHighlightsAfterMessageCount { get; }
    
    TimeSpan ResumeHighlightsAfterInterval { get; }
}