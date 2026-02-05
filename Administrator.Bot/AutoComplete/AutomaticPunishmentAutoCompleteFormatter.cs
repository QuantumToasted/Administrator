using Administrator.Core;
using Administrator.Database;
using Qmmands;

namespace Administrator.Bot.AutoComplete;

public sealed class AutomaticPunishmentAutoCompleteFormatter : IAutoCompleteFormatter<AutomaticPunishment, int>
{
    public static string FormatAutoCompleteName(ICommandContext context, AutomaticPunishment model) => model.FormatValue();
    public static int FormatAutoCompleteValue(ICommandContext context, AutomaticPunishment model) => model.DemeritPoints;
    public static string[] FormatComparisonValues(ICommandContext context, AutomaticPunishment model) => [model.DemeritPoints.ToString()];
}