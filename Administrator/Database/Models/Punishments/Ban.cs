namespace Administrator.Database
{
    public sealed class Ban : RevocablePunishment
    {
        public Ban(ulong guildId, ulong targetId, ulong moderatorId, string reason) 
            : base(guildId, targetId, moderatorId, reason, true)
        { }
    }
}