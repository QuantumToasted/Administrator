using System.Text;
using Administrator.Core;
using Administrator.Database;
using Qmmands;

namespace Administrator.Bot.AutoComplete;

public sealed class ForumAutoTagAutoCompleteFormatter : IAutoCompleteFormatter<ForumAutoTag, int>
{
    public static string FormatAutoCompleteName(ICommandContext context, ForumAutoTag model)
    {
        return new StringBuilder($"#{model.Id}")
            .Append(" - matching ")
            .Append(model.IsRegex ? "regex " : string.Empty)
            .Append($"'{model.Text}'")
            .ToString();
    }

    public static int FormatAutoCompleteValue(ICommandContext context, ForumAutoTag model) => model.Id;

    public static string[] FormatComparisonValues(ICommandContext context, ForumAutoTag model) => [model.Text, model.Id.ToString()];
}