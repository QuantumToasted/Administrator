using Administrator.Common;

namespace Administrator.Database
{
    public sealed class LoggingChannel
    {
        private LoggingChannel()
        { }

        public LoggingChannel(ulong id, LogType type)
        {
            Id = id;
            Type = type;
        }

        public ulong Id { get; set; }

        public ulong GuildId { get; set; }

        public LogType Type { get; set; }
    }
}