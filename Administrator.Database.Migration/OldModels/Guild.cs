namespace Administrator.Database.Migration
{
    [Flags]
    public enum OldGuildSettings
    {
        Punishments = 1,
        Modmail = 2,
        AutoPunishments = 4,
        XpTracking = 8,
        InviteFiltering = 16
    }
    
    public partial class Guild
    {
        public decimal Id { get; set; }
        public string? ApiKey { get; set; }
        public string[]? CustomPrefixes { get; set; }
        public string? BlacklistedModmailAuthors { get; set; }
        public string? BlacklistedStarboardIds { get; set; }
        public int Settings { get; set; }
        public TimeSpan XpGainInterval { get; set; }
        public int XpRate { get; set; }
        public int LevelUpWhitelist { get; set; }
        public int MaximumReactionRoles { get; set; }
        public int BigEmojiSize { get; set; }
        public int MinimumStars { get; set; }
        public string? Greeting { get; set; }
        public bool DmGreeting { get; set; }
        public TimeSpan? GreetingDuration { get; set; }
        public string? Goodbye { get; set; }
        public TimeSpan? GoodbyeDuration { get; set; }
        public int PruneDays { get; set; }
        public string? BlacklistedMessageFilterIds { get; set; }
    }
}
