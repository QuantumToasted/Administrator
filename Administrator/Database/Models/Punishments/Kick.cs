namespace Administrator.Database
{
    public sealed class Kick : Punishment
    {
        public Kick(ulong guildId, ulong targetId, ulong moderatorId, string reason) 
            : base(guildId, targetId, moderatorId, reason)
        { }
    }
}