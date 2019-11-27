using Administrator.Common;

namespace Administrator.Database
{
    public sealed class SpecialRole
    {
        private SpecialRole()
        { }

        public SpecialRole(ulong id, ulong guildId, RoleType type)
        {
            Id = id;
            GuildId = guildId;
            Type = type;
        }

        public ulong Id { get; set; }

        public ulong GuildId { get; set; }

        public RoleType Type { get; set; }
    }
}