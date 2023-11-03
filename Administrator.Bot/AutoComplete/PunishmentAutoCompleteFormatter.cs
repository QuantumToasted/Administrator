using System.Text;
using Administrator.Core;
using Administrator.Database;
using Disqord;

namespace Administrator.Bot.AutoComplete;

public sealed class PunishmentAutoCompleteFormatter : IAutoCompleteFormatter<Punishment, int>
{
    public string[] FormatAutoCompleteNames(IClient client, Punishment model)
    {
        var builder = new StringBuilder($"#{model.Id} - ")
            .Append($"{model.FormatPunishmentName()} | ")
            .Append($"Target: {model.Target.Name}");

        if (!string.IsNullOrWhiteSpace(model.Reason))
            builder.Append($" | {model.Reason}");

        return new[] { builder.ToString() };
    }

    public int FormatAutoCompleteValue(IClient client, Punishment model)
        => model.Id;

    public Func<Punishment, string[]> ComparisonSelector => punishment => new[] { punishment.Id.ToString() };
}