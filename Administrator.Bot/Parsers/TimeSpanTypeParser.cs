using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Administrator.Core;
using Disqord.Bot.Commands;
using Qmmands;

namespace Administrator.Bot;

public sealed class TimeSpanTypeParser : DiscordTypeParser<TimeSpan>
{
    private static readonly Regex PeriodRegex = new(
        @"((0|[1-9]\d*)(\.\d+)?)(y(?:ears|ear?)?|mo(?:nths|nth?)?|w(?:eeks|eek?)?|d(?:ays|ay?)?|h(?:ours|rs|r?)|m(?:inutes|ins|in?)?|s(?:econds|econd|ecs|ec?)?)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    /* old
    private static readonly Regex TimeSpanRegex = new(
        @"(\d+)(y(?:ears|ear?)?|mo(?:nths|nth?)?|w(?:eeks|eek?)?|d(?:ays|ay?)?|h(?:ours|rs|r?)|m(?:inutes|ins|in?)?|s(?:econds|econd|ecs|ec?)?)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    */

    public override ValueTask<ITypeParserResult<TimeSpan>> ParseAsync(IDiscordCommandContext context, IParameter parameter, ReadOnlyMemory<char> value)
    {
        return TryParse(value.ToString(), out var result)
            //? result <= TimeSpan.FromDays(365 * 5)
                ? Success(result.Value)
            //    : Failure("Timespans cannot exceed 5 years.")
            : Failure($"The supplied time span string \"{value}\" was invalid, improperly formatted, or too long.");
    }

    public static bool TryParse(string value, [NotNullWhen(true)] out TimeSpan? ts)
    {
        ts = null;

        var start = DateTimeOffset.UtcNow;
        var end = start;

        foreach (var match in PeriodRegex.Matches(value).Cast<Match>())
        {
            if (!double.TryParse(match.Groups[1].Value, out var doubleAmount) || doubleAmount <= 0)
                continue;
            
            // TODO: Originally double/fractional values were intended to be allowed (IE, 1.25days).
            // NodaTime does not support them, but this prevents 1.25days being interpreted as 25 days, so we just truncate and drop decimals.

            var amount = (int) doubleAmount;

            var character = match.Groups[4].Value[0];
            var isMonth = match.Groups[4].Value.Length > 1 && match.Groups[4].Value[1] == 'o';

            try
            {
                end = character switch
                {
                    'y' => end.AddYears(amount),
                    'm' when isMonth => end.AddMonths(amount),
                    'w' => end.AddWeeks(amount),
                    'd' => end.AddDays(amount),
                    'h' => end.AddHours(amount),
                    'm' => end.AddMinutes(amount),
                    's' => end.AddSeconds(amount),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
            catch
            {
                return false;
            }
        }

        ts = end > start ? end - start : null;
        return end > start;
    }
}