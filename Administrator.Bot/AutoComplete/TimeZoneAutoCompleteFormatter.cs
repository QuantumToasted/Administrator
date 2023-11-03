using Administrator.Core;
using Disqord;

namespace Administrator.Bot.AutoComplete;

public sealed class TimeZoneAutoCompleteFormatter : IAutoCompleteFormatter<TimeZoneInfo, string>
{
    public string[] FormatAutoCompleteNames(IClient client, TimeZoneInfo model)
        => new[] { model.Id };

    public string FormatAutoCompleteValue(IClient client, TimeZoneInfo model)
        => model.Id;

    public Func<TimeZoneInfo, string[]> ComparisonSelector => static tz => new[] { tz.Id };
}