using Administrator.Core;
using Backpack.Net;

namespace Administrator.Bot.AutoComplete;

public sealed class ItemAutoCompleteFormatter : IAutoCompleteFormatter<ItemAutoCompleteFormatter.TF2Item, string>
{
    public sealed record TF2Item(string Name, Item Item);

    public string FormatAutoCompleteName(TF2Item model)
        => model.Name;

    public string FormatAutoCompleteValue(TF2Item model)
        => model.Name;

    public Func<TF2Item, string[]> ComparisonSelector => static model => [model.Name];
}