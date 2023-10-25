namespace Administrator.Database.Migration
{
    public partial class Tag
    {
        public decimal GuildId { get; set; }
        public string Name { get; set; } = null!;
        public decimal AuthorId { get; set; }
        public string? Response { get; set; }
        public byte[]? Image { get; set; }
        public short Format { get; set; }
        public DateTime CreatedAt { get; set; }
        public int Uses { get; set; }
    }
}
