using Administrator.Core;
using Disqord;
using Qmmands;

namespace Administrator.Bot.AutoComplete;

public sealed class ForumTagAutoCompleteFormatter : IAutoCompleteFormatter<IForumTag, string>
{
    public static string FormatAutoCompleteName(ICommandContext context, IForumTag model) => model.Name;

    public static string FormatAutoCompleteValue(ICommandContext context, IForumTag model) => model.Id.ToString();

    public static string[] FormatComparisonValues(ICommandContext context, IForumTag model) => [model.Name, model.Id.ToString()];
}