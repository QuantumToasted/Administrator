using Administrator.Core;
using Qmmands;

namespace Administrator.Bot.AutoComplete;

public sealed class TimeZoneAutoCompleteFormatter : IAutoCompleteFormatter<TimeZoneInfo, string>
{
    public static string FormatAutoCompleteName(ICommandContext context, TimeZoneInfo model) => model.Id;
    public static string FormatAutoCompleteValue(ICommandContext context, TimeZoneInfo model) => model.Id;
    public static string[] FormatComparisonValues(ICommandContext context, TimeZoneInfo model) => [model.Id];
}