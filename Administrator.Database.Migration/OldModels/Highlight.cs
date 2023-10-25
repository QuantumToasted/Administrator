namespace Administrator.Database.Migration
{
    public partial class Highlight
    {
        public int Id { get; set; }
        public decimal UserId { get; set; }
        public string? Text { get; set; }
        public decimal? GuildId { get; set; }
    }
}
