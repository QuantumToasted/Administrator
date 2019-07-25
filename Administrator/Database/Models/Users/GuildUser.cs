namespace Administrator.Database
{
    public sealed class GuildUser : User
    {
        public GuildUser(ulong id, ulong guildId)
            : base(id)
        {
            GuildId = guildId;
        }

        public ulong GuildId { get; set; }
    }
}