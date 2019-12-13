using System;

namespace Administrator.Database
{
    public sealed class Reminder
    {
        private Reminder()
        { }

        public Reminder(ulong authorId, ulong? guildId, ulong channelId, ulong messageId, string text,
            TimeSpan duration)
        {
            AuthorId = authorId;
            GuildId = guildId;
            ChannelId = channelId;
            MessageId = messageId;
            Text = text;
            CreatedAt = DateTimeOffset.UtcNow;
            Ending = CreatedAt + duration;
        }

        public int Id { get; set; }

        public ulong AuthorId { get; set; }

        public ulong? GuildId { get; set; }

        public ulong ChannelId { get; set; }

        public ulong MessageId { get; set; }

        public string Text { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset Ending { get; set; }
    }
}