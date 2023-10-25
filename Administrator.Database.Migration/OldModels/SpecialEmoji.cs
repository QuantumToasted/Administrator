namespace Administrator.Database.Migration
{
    public enum OldEmojiType
    {
        Upvote,
        Downvote,
        LevelUp,
        Star
    }
    
    public partial class SpecialEmoji
    {
        public decimal GuildId { get; set; }
        public int Type { get; set; }
        public string? Emoji { get; set; }
    }
}
