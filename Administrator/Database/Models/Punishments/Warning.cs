namespace Administrator.Database
{
    public sealed class Warning : RevocablePunishment
    {
        public Warning(ulong guildId, ulong targetId, ulong moderatorId, string reason) 
            : base(guildId, targetId, moderatorId, reason, false)
        { }

        public int? SecondaryPunishmentId{ get; set; }

        public void SetSecondaryPunishment(Punishment punishment)
        {
            SecondaryPunishmentId = punishment.Id;
        }
    }
}