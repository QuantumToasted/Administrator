using System.Text;
using Administrator.Core;
using Administrator.Database;
using Disqord;
using Humanizer;
using Humanizer.Localisation;
using Qmmands;

namespace Administrator.Bot.AutoComplete;

public sealed class AutomaticPunishmentAutoCompleteFormatter : IAutoCompleteFormatter<AutomaticPunishment, int>
{
    public string FormatAutoCompleteName(AutomaticPunishment model)
    {
        return FormatWarningPunishment(model);
    }

    public static string FormatWarningPunishment(AutomaticPunishment automaticPunishment, bool includeCount = true)
    {
        var builder = new StringBuilder();

        if (includeCount)
            builder.Append($"{automaticPunishment.DemeritPoints} - ");

        builder.Append(automaticPunishment.PunishmentType switch
        {
            PunishmentType.Ban when automaticPunishment.PunishmentDuration is not null =>
                $"Ban with duration of {automaticPunishment.PunishmentDuration.Value.Humanize(int.MaxValue, maxUnit: TimeUnit.Year, minUnit: TimeUnit.Second)}",
            PunishmentType.Ban =>
                "Permanent ban",
            PunishmentType.Timeout =>
                $"Timeout with duration of {automaticPunishment.PunishmentDuration!.Value.Humanize(int.MaxValue, maxUnit: TimeUnit.Year, minUnit: TimeUnit.Second)}",
            PunishmentType.Kick =>
                "Kick",
            _ => throw new ArgumentOutOfRangeException()
        });

        return builder.ToString();
    }

    public int FormatAutoCompleteValue(AutomaticPunishment model)
        => model.DemeritPoints;

    public Func<AutomaticPunishment, string[]> ComparisonSelector => static warningPunishment => [warningPunishment.DemeritPoints.ToString()];
}