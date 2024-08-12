using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Administrator.Core;
using Disqord.Bot.Commands;
using Qmmands;

namespace Administrator.Bot;

public sealed class TimeSpanTypeParser : DiscordTypeParser<TimeSpan>
{
    private static readonly Regex TimeSpanRegex = new(
        @"(\d+)(y(?:ears|ear?)?|mo(?:nths|nth?)?|w(?:eeks|eek?)?|d(?:ays|ay?)?|h(?:ours|rs|r?)|m(?:inutes|ins|in?)?|s(?:econds|econd|ecs|ec?)?)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public override ValueTask<ITypeParserResult<TimeSpan>> ParseAsync(IDiscordCommandContext context, IParameter parameter, ReadOnlyMemory<char> value)
    {
        return TryParse(value.ToString(), out var result)
            ? Success(result.Value)
            : Failure($"The supplied time span string \"{value}\" was invalid, improperly formatted, or too long.");
    }

    public static bool TryParse(string value, [NotNullWhen(true)] out TimeSpan? ts)
    {
        value = value.Replace(" ", ""); // TODO: is removing spaces a good idea here? 15 seconds -> 15seconds
        
        ts = null;

        var start = DateTimeOffset.UtcNow;
        var end = start;

        foreach (var match in TimeSpanRegex.Matches(value).Cast<Match>())
        {
            if (!int.TryParse(match.Groups[1].Value, out var amount) || amount <= 0)
                continue;

            var character = match.Groups[2].Value[0];
            var isMonth = match.Groups[2].Value.Length > 1 && match.Groups[2].Value[1] == 'o';

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