using System;
using Disqord;

namespace Administrator.Database
{
    public abstract class Punishment
    {
        private Punishment()
        { }

        protected Punishment(ulong guildId, ulong targetId, ulong moderatorId, string reason)
        {
            GuildId = guildId;
            TargetId = targetId;
            ModeratorId = moderatorId;
            Reason = reason;
            CreatedAt = DateTimeOffset.UtcNow;
        }

        public int Id { get; set; }

        public ulong GuildId { get; set; }

        public ulong TargetId { get; set; }

        public ulong ModeratorId { get; set; }

        public string Reason { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public ulong LogMessageId { get; set; }

        public ulong LogMessageChannelId { get; set; }

        public void SetLogMessage(IUserMessage message)
        {
            LogMessageId = message.Id;
            LogMessageChannelId = message.ChannelId;
        }
    }
}