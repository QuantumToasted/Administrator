using Administrator.Core;
using Backpack.Net;
using Qmmands;

namespace Administrator.Bot.AutoComplete;

public sealed class ItemAutoCompleteFormatter : IAutoCompleteFormatter<ItemAutoCompleteFormatter.TF2Item, string>
{
    public static string FormatAutoCompleteName(ICommandContext context, TF2Item model) => model.Name;
    public static string FormatAutoCompleteValue(ICommandContext context, TF2Item model) => model.Name;
    public static string[] FormatComparisonValues(ICommandContext context, TF2Item model) => [model.Name];
    public sealed record TF2Item(string Name, Item Item);
}