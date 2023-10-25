namespace Administrator.Database.Migration
{
    public partial class GuildUser
    {
        public decimal Id { get; set; }
        public decimal GuildId { get; set; }
        public string[]? PreviousNames { get; set; }
        public int TotalXp { get; set; }
        public DateTime LastXpGain { get; set; }
        public DateTime LastLevelUp { get; set; }
    }
}
