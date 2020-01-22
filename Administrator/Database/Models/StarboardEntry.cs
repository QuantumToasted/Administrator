using System;
using System.Collections.Generic;
using System.Linq;
using Disqord;

namespace Administrator.Database
{
    public sealed class StarboardEntry
    {
        private StarboardEntry()
        { }

        public StarboardEntry(ulong messageId, ulong channelId, ulong guildId, ulong authorId,
            IEnumerable<ulong> stars, ulong entryMessageId, ulong entryChannelId)
        {
            MessageId = messageId;
            ChannelId = channelId;
            GuildId = guildId;
            AuthorId = authorId;
            Stars = stars.ToList();
            EntryMessageId = entryMessageId;
            EntryChannelId = entryChannelId;
        }

        public ulong MessageId { get; set; }

        public ulong ChannelId { get; set; }

        public ulong GuildId { get; set; }

        public ulong AuthorId { get; set; }

        public List<ulong> Stars { get; set; }

        public ulong EntryMessageId { get; set; }

        public ulong EntryChannelId { get; set; }

        public string JumpUrl => $"https://discordapp.com/channels/{GuildId}/{ChannelId}/{MessageId}";

        public DateTimeOffset Timestamp => new Snowflake(MessageId).CreatedAt;

        public void SetEntryIds(IUserMessage message)
        {
            EntryMessageId = message.Id;
            EntryChannelId = message.ChannelId;
        }
    }
}