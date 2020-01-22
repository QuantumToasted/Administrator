using Disqord;

namespace Administrator.Database
{
    public sealed class CyclingStatus
    {
        private CyclingStatus()
        { }

        public CyclingStatus(ActivityType type, string text)
        {
            Type = type;
            Text = text;
        }

        public int Id { get; set; }

        public ActivityType Type { get; set; }

        public string Text { get; set; }
    }
}