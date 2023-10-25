namespace Administrator.Database.Migration
{
    public partial class BigEmoji
    {
        public decimal Id { get; set; }
        public decimal GuildId { get; set; }
        public string Discriminator { get; set; } = null!;
        public decimal? DenierId { get; set; }
        public DateTime? DeniedAt { get; set; }
        public int? Uses { get; set; }
        public decimal? RequesterId { get; set; }
        public DateTime? RequestedAt { get; set; }
    }
}
