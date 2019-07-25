using System;

namespace Administrator.Database
{
    public sealed class Suggestion
    {
        private Suggestion()
        { }

        public Suggestion(ulong guildId, ulong userId, string text)
        {
            GuildId = guildId;
            UserId = userId;
            Text = text;
        }

        public int Id { get; set; }

        public ulong GuildId { get; set; }

        public ulong UserId { get; set; }

        public string Text { get; set; }

        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

        public ulong MessageId { get; set; }

        public void SetMessageId(ulong messageId)
        {
            MessageId = messageId;
        }
    }
}