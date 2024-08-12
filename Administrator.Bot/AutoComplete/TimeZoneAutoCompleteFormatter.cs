using Administrator.Core;
using Disqord;

namespace Administrator.Bot.AutoComplete;

public sealed class TimeZoneAutoCompleteFormatter : IAutoCompleteFormatter<TimeZoneInfo, string>
{
    public string FormatAutoCompleteName(TimeZoneInfo model)
        => model.Id;

    public string FormatAutoCompleteValue(TimeZoneInfo model)
        => model.Id;

    public Func<TimeZoneInfo, string[]> ComparisonSelector => static model => [model.Id];
}