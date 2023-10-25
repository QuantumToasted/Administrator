namespace Administrator.Database.Migration
{
    [Flags]
    public enum OldTextChannelSettings
    {
        SendCommandErrors = 1,
        DeleteCommandMessages = 2,
        XpTracking = 4
    }
    
    public partial class TextChannel
    {
        public decimal Id { get; set; }
        public int Settings { get; set; }
    }
}
