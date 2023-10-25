namespace Administrator.Database.Migration
{
    public partial class Cooldown
    {
        public decimal GuildId { get; set; }
        public string CommandName { get; set; } = null!;
        public TimeSpan Cooldown1 { get; set; }
    }
}
