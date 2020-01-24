namespace Administrator.Database
{
    public sealed class SelfAssignableRole
    {
        private SelfAssignableRole()
        { }

        public SelfAssignableRole(ulong guildId, ulong roleId)
        {
            GuildId = guildId;
            RoleId = roleId;
        }

        public ulong GuildId { get; set; }

        public ulong RoleId { get; set; }

        public int[] Groups { get; set; } = new int[0];
    }
}