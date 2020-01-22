using System;

namespace Administrator.Database
{
    public sealed class CommandCooldown
    {
        public CommandCooldown(ulong guildId, string commandName, TimeSpan cooldown)
        {
            GuildId = guildId;
            CommandName = commandName;
            Cooldown = cooldown;
        }

        public ulong GuildId { get; set; }

        public string CommandName { get; set; }

        public TimeSpan Cooldown { get; set; }
    }
}