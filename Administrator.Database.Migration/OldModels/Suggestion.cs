namespace Administrator.Database.Migration
{
    public partial class Suggestion
    {
        public int Id { get; set; }
        public decimal GuildId { get; set; }
        public decimal AuthorId { get; set; }
        public string? Text { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal MessageId { get; set; }
        public decimal ChannelId { get; set; }
        public byte[]? Image { get; set; }
        public short Format { get; set; }
        public decimal? ModifierId { get; set; }
        public string? ModificationReason { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public bool? IsApproved { get; set; }
    }
}
