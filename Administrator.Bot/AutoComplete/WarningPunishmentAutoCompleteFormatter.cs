using System.Text;
using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Bot;
using Humanizer;
using Humanizer.Localisation;

namespace Administrator.Bot.AutoComplete;

public sealed class WarningPunishmentAutoCompleteFormatter : IAutoCompleteFormatter<WarningPunishment, int>
{
    public string FormatAutoCompleteName(IClient client, WarningPunishment model) => FormatWarningPunishment(model);

    public static string FormatWarningPunishment(WarningPunishment warningPunishment)
    {
        var builder = new StringBuilder($"{warningPunishment.WarningCount} - ")
            .Append(warningPunishment.PunishmentType switch
            {
                PunishmentType.Ban when warningPunishment.PunishmentDuration is not null =>
                    $"Ban with duration of {warningPunishment.PunishmentDuration.Value.Humanize(int.MaxValue, maxUnit: TimeUnit.Year, minUnit: TimeUnit.Second)}",
                PunishmentType.Ban =>
                    "Permanent ban",
                PunishmentType.Timeout =>
                    $"Timeout with duration of {warningPunishment.PunishmentDuration!.Value.Humanize(int.MaxValue, maxUnit: TimeUnit.Year, minUnit: TimeUnit.Second)}",
                PunishmentType.Kick =>
                    "Kick",
                _ => throw new ArgumentOutOfRangeException()
            });

        return builder.ToString();
    }

    public int FormatAutoCompleteValue(IClient client, WarningPunishment model)
        => model.WarningCount;

    public Func<WarningPunishment, string> ComparisonSelector => warningPunishment => warningPunishment.WarningCount.ToString();
}