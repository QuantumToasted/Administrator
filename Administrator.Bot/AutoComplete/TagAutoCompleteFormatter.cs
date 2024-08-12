using Administrator.Core;
using Administrator.Database;
using Disqord;

namespace Administrator.Bot.AutoComplete;

public sealed class TagAutoCompleteFormatter : IAutoCompleteFormatter<Tag, string>
{
    public string FormatAutoCompleteName(Tag model)
        => model.Name;

    public string FormatAutoCompleteValue(Tag model)
        => model.Name;

    public Func<Tag, string[]> ComparisonSelector => static model => model.Aliases.Append(model.Name).ToArray();
}