using Administrator.Core;
using Administrator.Database;
using Disqord;

namespace Administrator.Bot.AutoComplete;

public sealed class TagAutoCompleteFormatter : IAutoCompleteFormatter<Tag, string>
{
    public string FormatAutoCompleteName(IClient client, Tag model)
        => model.Name;

    public string FormatAutoCompleteValue(IClient client, Tag model)
        => model.Name;

    public Func<Tag, string> ComparisonSelector => static tag => tag.Name;
}