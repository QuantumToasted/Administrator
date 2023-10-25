namespace Administrator.Database.Migration
{
    public enum OldPunishmentType
    {
        Mute,
        Kick,
        Ban
    }
    
    public partial class WarningPunishment
    {
        public decimal GuildId { get; set; }
        public int Count { get; set; }
        public int Type { get; set; }
        public TimeSpan? Duration { get; set; }
    }
}
