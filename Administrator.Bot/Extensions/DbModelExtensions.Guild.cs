using Administrator.Database;
using Qommon;

namespace Administrator.Bot;

public static partial class DbModelExtensions
{
    public static LoggingChannel? GetLoggingChannel(this Guild guild, LogEventType type)
    {
        Guard.IsNotNull(guild.LoggingChannels);
        return guild.LoggingChannels.FirstOrDefault(x => x.EventType == type);
    }
}