namespace Administrator.Database.Migration
{
    public partial class CooldownDatum
    {
        public decimal GuildId { get; set; }
        public decimal UserId { get; set; }
        public string Command { get; set; } = null!;
        public DateTime LastRun { get; set; }
    }
}
