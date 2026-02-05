using Administrator.Core;
using Administrator.Database;
using Qmmands;

namespace Administrator.Bot.AutoComplete;

public sealed class LuaCommandAutoCompleteFormatter : IAutoCompleteFormatter<LuaCommand, string>
{
    public static string FormatAutoCompleteName(ICommandContext context, LuaCommand model) => model.Name;
    public static string FormatAutoCompleteValue(ICommandContext context, LuaCommand model) => model.Name;
    public static string[] FormatComparisonValues(ICommandContext context, LuaCommand model) => [model.Name];
}