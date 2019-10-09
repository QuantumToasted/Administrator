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

        public override bool Equals(object obj)
        {
            if (obj is GuildUser other)
            {
                return other.Id == Id && other.GuildId == GuildId;
            }

            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + Id.GetHashCode();
                hash = hash * 31 + GuildId.GetHashCode();
                return hash;
            }
        }
    }
}