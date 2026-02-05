using System.Text.RegularExpressions;
using Administrator.Database;
using Chronic.Core;
using Disqord.Bot.Commands;
using Qmmands;

namespace Administrator.Bot;

public sealed partial class DateTimeOffsetTypeParser : DiscordTypeParser<DateTimeOffset>
{
    private static readonly Regex TimestampMarkdownRegex = GenerateTimestampRegex();
    
    public override async ValueTask<ITypeParserResult<DateTimeOffset>> ParseAsync(IDiscordCommandContext context, IParameter parameter, ReadOnlyMemory<char> value)
    {
        var input = value.ToString();
        var match = TimestampMarkdownRegex.Match(input);

        if (match.Success)
        {
            var unixSecondsTimestamp = long.Parse(match.Groups["time"].Value);
            return Success(DateTimeOffset.FromUnixTimeSeconds(unixSecondsTimestamp));
        }
        
        await using var scope = context.Services.CreateAsyncScopeWithDatabase(out var db);
        var globalUser = await db.Users.GetOrCreateAsync(context.AuthorId);

        var now = DateTimeOffset.UtcNow;

        if (TimeSpanTypeParser.TryParse(input, out var duration))
        {
            return Success(now + duration.Value);
        }

        var timeZone = globalUser.GetTimeZone();
        var parser = new Parser(new Options { Clock = () => TimeZoneInfo.ConvertTime(now, timeZone).DateTime });

        if (parser.Parse(input) is { } span)
        {
            var dateTime = DateTime.SpecifyKind(span.ToTime(), DateTimeKind.Unspecified);
            return Success(new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(dateTime, timeZone)));
        }

        return Failure("Failed to convert your input into a valid instance in time.\n" +
                       "Some examples to help you better format it:\n" +
                       "\"tomorrow at 8pm\"\n" +
                       "\"3h50m\"\n" +
                       "\"one week from now\"\n" +
                       "\"in 30 minutes\"");
    }

    [GeneratedRegex(@"<t:(?<time>[\d]+)(:[a-z])?>")]
    private static partial Regex GenerateTimestampRegex();
}