namespace Administrator.Database.Migration
{
    public partial class ReactionRole
    {
        public int Id { get; set; }
        public decimal GuildId { get; set; }
        public decimal ChannelId { get; set; }
        public decimal MessageId { get; set; }
        public decimal RoleId { get; set; }
        public string? Emoji { get; set; }
    }
}
