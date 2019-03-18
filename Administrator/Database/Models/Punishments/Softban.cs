namespace Administrator.Database
{
    public sealed class Softban : Punishment
    {
        public Softban(ulong guildId, ulong targetId, ulong moderatorId, string reason) 
            : base(guildId, targetId, moderatorId, reason)
        { }
    }
}