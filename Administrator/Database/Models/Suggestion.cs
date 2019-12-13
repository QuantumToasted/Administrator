using System;
using System.IO;
using Disqord;

namespace Administrator.Database
{
    public sealed class Suggestion
    {
        private Suggestion()
        { }

        public Suggestion(ulong guildId, ulong userId, string text, MemoryStream image, ImageFormat format)
        {
            GuildId = guildId;
            UserId = userId;
            Text = text;
            Image = image;
            Format = format;
        }

        public int Id { get; set; }

        public ulong GuildId { get; set; }

        public ulong UserId { get; set; }

        public string Text { get; set; }

        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

        public ulong MessageId { get; set; }

        public MemoryStream Image { get; set; } = new MemoryStream();

        public ImageFormat Format { get; set; } = ImageFormat.Default;

        public void SetMessageId(ulong messageId)
        {
            MessageId = messageId;
        }
    }
}