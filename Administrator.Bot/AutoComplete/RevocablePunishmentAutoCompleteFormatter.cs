using System.Text;
using Administrator.Core;
using Administrator.Database;

namespace Administrator.Bot.AutoComplete;

public class RevocablePunishmentAutoCompleteFormatter : IAutoCompleteFormatter<RevocablePunishment, int>
{
    public string FormatAutoCompleteName(RevocablePunishment model)
    {
        var builder = new StringBuilder($"#{model.Id} - ")
            .Append($"{model.FormatPunishmentName()} | ")
            .Append($"Target: {model.Target.Name}");

        if (!string.IsNullOrWhiteSpace(model.Reason))
            builder.Append($" | {model.Reason}");

        return builder.ToString();
    }

    public int FormatAutoCompleteValue(RevocablePunishment model)
        => model.Id;

    public Func<RevocablePunishment, string[]> ComparisonSelector => static model => [model.Id.ToString()];
}