using System.Text;
using Administrator.Core;
using Administrator.Database;

namespace Administrator.Bot.AutoComplete;

public sealed class PunishmentAutoCompleteFormatter : IAutoCompleteFormatter<Punishment, int>
{
    public string FormatAutoCompleteName(Punishment model)
    {
        var builder = new StringBuilder($"#{model.Id} - ")
            .Append($"{model.FormatPunishmentName()} | ")
            .Append($"Target: {model.Target.Name}");

        if (!string.IsNullOrWhiteSpace(model.Reason))
            builder.Append($" | {model.Reason}");

        return builder.ToString();
    }

    public int FormatAutoCompleteValue(Punishment model)
        => model.Id;

    public Func<Punishment, string[]> ComparisonSelector => static model => [model.Id.ToString()];
}