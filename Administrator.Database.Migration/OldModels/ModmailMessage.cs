namespace Administrator.Database.Migration
{
    public partial class ModmailMessage
    {
        public int Id { get; set; }
        public int Target { get; set; }
        public string? Text { get; set; }
        public int SourceId { get; set; }
        public DateTime Timestamp { get; set; }

        public virtual Modmail Source { get; set; } = null!;
    }
}
