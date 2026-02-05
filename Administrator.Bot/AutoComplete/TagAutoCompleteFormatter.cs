using Administrator.Core;
using Administrator.Database;
using Qmmands;

namespace Administrator.Bot.AutoComplete;

public sealed class TagAutoCompleteFormatter : IAutoCompleteFormatter<Tag, string>
{
    public static string FormatAutoCompleteName(ICommandContext context, Tag model) => model.Name;
    public static string FormatAutoCompleteValue(ICommandContext context, Tag model) => model.Name;
    public static string[] FormatComparisonValues(ICommandContext context, Tag model) => [..model.Aliases, model.Name];
}