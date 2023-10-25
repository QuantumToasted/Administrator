namespace Administrator.Database.Migration
{
    public enum OldImageFormat : byte
    {
        Default,
        Png,
        Jpg,
        WebP,
        Gif,
    }
    
    public partial class Punishment
    {
        public int Id { get; set; }
        public decimal GuildId { get; set; }
        public decimal TargetId { get; set; }
        public string? TargetName { get; set; }
        public decimal ModeratorId { get; set; }
        public string? ModeratorName { get; set; }
        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal LogMessageId { get; set; }
        public decimal LogMessageChannelId { get; set; }
        public byte[]? Image { get; set; }
        public short Format { get; set; }
        public string Discriminator { get; set; } = null!;
        public DateTime? RevokedAt { get; set; }
        public decimal? RevokerId { get; set; }
        public string? RevokerName { get; set; }
        public string? RevocationReason { get; set; }
        public DateTime? AppealedAt { get; set; }
        public string? AppealReason { get; set; }
        public TimeSpan? Duration { get; set; }
        public decimal? ChannelId { get; set; }
        public decimal? PreviousChannelAllowValue { get; set; }
        public decimal? PreviousChannelDenyValue { get; set; }
        public int? SecondaryPunishmentId { get; set; }
    }
}
