namespace Administrator.Database.Migration
{
    public partial class LevelReward
    {
        public int Id { get; set; }
        public decimal GuildId { get; set; }
        public int Level { get; set; }
        public int Tier { get; set; }
        public string Discriminator { get; set; } = null!;
        public string? AddedRoleIds { get; set; }
        public string? RemovedRoleIds { get; set; }
    }
}
