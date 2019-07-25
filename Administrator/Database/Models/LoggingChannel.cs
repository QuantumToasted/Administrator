using Administrator.Common;

namespace Administrator.Database
{
    public sealed class LoggingChannel
    {
        private LoggingChannel()
        { }

        public LoggingChannel(ulong id, ulong guildId, LogType type)
        {
            Id = id;
            GuildId = guildId;
            Type = type;
        }

        public ulong Id { get; set; }

        public ulong GuildId { get; set; }

        public LogType Type { get; set; }
    }
}