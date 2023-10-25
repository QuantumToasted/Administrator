namespace Administrator.Database.Migration
{
    public partial class Starboard
    {
        public decimal MessageId { get; set; }
        public decimal ChannelId { get; set; }
        public decimal GuildId { get; set; }
        public decimal AuthorId { get; set; }
        public string? Stars { get; set; }
        public decimal EntryMessageId { get; set; }
        public decimal EntryChannelId { get; set; }
    }
}
