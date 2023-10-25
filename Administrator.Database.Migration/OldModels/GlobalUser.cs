namespace Administrator.Database.Migration
{
    public partial class GlobalUser
    {
        public decimal Id { get; set; }
        public string[]? PreviousNames { get; set; }
        public int TotalXp { get; set; }
        public DateTime LastXpGain { get; set; }
        public DateTime LastLevelUp { get; set; }
        public int LevelUpPreferences { get; set; }
        public string? HighlightBlacklist { get; set; }
        public string? DisplayName { get; set; }
    }
}
