using Administrator.Common;

namespace Administrator.Database
{
    public sealed class SpecialRole
    {
        private SpecialRole()
        { }

        public ulong Id { get; set; }

        public ulong GuildId { get; set; }

        public RoleType Type { get; set; }
    }
}