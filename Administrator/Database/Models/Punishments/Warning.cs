namespace Administrator.Database
{
    public sealed class Warning : RevocablePunishment
    {
        public Warning(ulong guildId, ulong targetId, ulong moderatorId, string reason) 
            : base(guildId, targetId, moderatorId, reason, false)
        { }
    }
}