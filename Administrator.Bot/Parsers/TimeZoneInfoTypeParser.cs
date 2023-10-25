using Administrator.Core;
using Disqord.Bot.Commands;
using Qmmands;

namespace Administrator.Bot;

public sealed class TimeZoneInfoTypeParser : DiscordTypeParser<TimeZoneInfo>
{
    public override ValueTask<ITypeParserResult<TimeZoneInfo>> ParseAsync(IDiscordCommandContext context, IParameter parameter, ReadOnlyMemory<char> value)
    {
        return DateTimeExtensions.IanaTimeZoneMap.TryGetValue(value.ToString(), out var timeZone)
            ? Success(timeZone)
            : Failure($"The supplied value \"{value}\" was unable to be converted to a timezone.");
    }
}