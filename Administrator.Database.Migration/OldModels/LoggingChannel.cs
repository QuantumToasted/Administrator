namespace Administrator.Database.Migration
{
    public enum OldLogType
    {
        Disable,
        Ban,
        Kick,
        Mute,
        Warn,
        Revoke,
        Modmail,
        Appeal,
        Suggestion,
        SuggestionArchive,
        MessageDelete,
        MessageUpdate,
        Join,
        Leave,
        UsernameUpdate,
        NicknameUpdate,
        AvatarUpdate,
        UserRoleUpdate,
        ReactionRemove,
        Starboard,
        Greeting,
        Goodbye,
        BotAnnouncements,
        Errors
    }
    
    public partial class LoggingChannel
    {
        public decimal GuildId { get; set; }
        public int Type { get; set; }
        public decimal Id { get; set; }
    }
}
