namespace Administrator.Database.Migration
{
    public partial class Reminder
    {
        public int Id { get; set; }
        public decimal AuthorId { get; set; }
        public decimal? ChannelId { get; set; }
        public decimal MessageId { get; set; }
        public string? Text { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime Ending { get; set; }
    }
}
