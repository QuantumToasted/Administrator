namespace Administrator.Database.Migration
{
    public partial class Modmail
    {
        public Modmail()
        {
            ModmailMessages = new HashSet<ModmailMessage>();
        }

        public int Id { get; set; }
        public decimal GuildId { get; set; }
        public decimal UserId { get; set; }
        public bool IsAnonymous { get; set; }
        public int? ClosedBy { get; set; }

        public virtual ICollection<ModmailMessage> ModmailMessages { get; set; }
    }
}
