using System;

namespace Administrator.Database
{
    public sealed class CooldownData
    {
        private CooldownData()
        { }

        public CooldownData(ulong guildId, ulong userId, string command, DateTimeOffset lastRun)
        {
            GuildId = guildId;
            UserId = userId;
            Command = command;
            LastRun = lastRun;
        }

        public ulong GuildId { get; set; }

        public ulong UserId { get; set; }

        public string Command { get; set; }

        public DateTimeOffset LastRun { get; set; }
    }
}