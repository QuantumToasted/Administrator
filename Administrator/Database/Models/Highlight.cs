namespace Administrator.Database
{
    public sealed class Highlight
    {
        private Highlight()
        { }

        public Highlight(ulong userId, string text, ulong? guildId)
        {
            UserId = userId;
            Text = text;
            GuildId = guildId;
        }

        public int Id { get; set; }

        public ulong UserId { get; set; }

        public string Text { get; set; }

        public ulong? GuildId { get; set; }
    }
}
