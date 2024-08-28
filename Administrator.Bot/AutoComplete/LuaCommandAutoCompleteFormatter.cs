using Administrator.Core;
using Administrator.Database;

namespace Administrator.Bot.AutoComplete;

public sealed class LuaCommandAutoCompleteFormatter : IAutoCompleteFormatter<LuaCommand, string>
{
    public string FormatAutoCompleteName(LuaCommand model)
        => model.Name;

    public string FormatAutoCompleteValue(LuaCommand model)
        => model.Name;

    public Func<LuaCommand, string[]> ComparisonSelector => static model => [model.Name];
}