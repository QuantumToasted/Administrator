namespace Administrator.Database.Migration
{
    public partial class MessageFilter
    {
        public int Id { get; set; }
        public decimal GuildId { get; set; }
        public string? Text { get; set; }
    }
}
