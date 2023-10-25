using Administrator.Database;
using Chronic.Core;
using Disqord.Bot.Commands;
using Qmmands;

namespace Administrator.Bot;

public sealed class DateTimeOffsetTypeParser : DiscordTypeParser<DateTimeOffset>
{
    public override async ValueTask<ITypeParserResult<DateTimeOffset>> ParseAsync(IDiscordCommandContext context, IParameter parameter, ReadOnlyMemory<char> value)
    {
        await using var scope = context.Services.CreateAsyncScopeWithDatabase(out var db);
        var globalUser = await db.GetOrCreateGlobalUserAsync(context.AuthorId);

        var input = value.ToString();
        var now = DateTimeOffset.UtcNow;

        if (TimeSpanTypeParser.TryParse(input, out var duration))
        {
            return Success(now + duration.Value);
        }
        
        var parser = new Parser(new Options{Clock = () => TimeZoneInfo.ConvertTime(now, globalUser.GetTimeZone()).LocalDateTime});

        if (parser.Parse(input) is { } span)
        {
            return Success(new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(span.ToTime(), globalUser.GetTimeZone())));
        }

        return Failure("Failed to convert your input into a valid instance in time.\n" +
                       "Some examples to help you better format it:\n" +
                       "\"tomorrow at 8pm\"\n" +
                       "\"3h50m\"\n" +
                       "\"one week from now\"\n" +
                       "\"in 30 minutes\"");
    }
}