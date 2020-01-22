using System;
using System.IO;
using Disqord;

namespace Administrator.Database
{
    public abstract class Punishment
    {
        private Punishment()
        { }

        protected Punishment(ulong guildId, ulong targetId, ulong moderatorId, string reason, MemoryStream image = null, ImageFormat format = ImageFormat.Default)
        {
            GuildId = guildId;
            TargetId = targetId;
            ModeratorId = moderatorId;
            Reason = reason;
            CreatedAt = DateTimeOffset.UtcNow;
            Image = image ?? new MemoryStream();
            Format = format;
        }

        public int Id { get; set; }

        public ulong GuildId { get; set; }

        public ulong TargetId { get; set; }

        public ulong ModeratorId { get; set; }

        public string Reason { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public ulong LogMessageId { get; set; }

        public ulong LogMessageChannelId { get; set; }

        public MemoryStream Image { get; set; } = new MemoryStream();

        public ImageFormat Format { get; set; } = ImageFormat.Default;

        public void SetLogMessage(IUserMessage message)
        {
            LogMessageId = message.Id;
            LogMessageChannelId = message.ChannelId;
        }
    }
}