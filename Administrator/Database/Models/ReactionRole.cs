using Disqord;

namespace Administrator.Database
{
    public sealed class ReactionRole
    {
        private ReactionRole()
        { }

        public ReactionRole(ulong guildId, ulong channelId, ulong messageId, ulong roleId, IEmoji emoji)
        {
            GuildId = guildId;
            ChannelId = channelId;
            MessageId = messageId;
            RoleId = roleId;
            Emoji = emoji;
        }

        public int Id { get; set; }

        public ulong GuildId { get; set; }

        public ulong ChannelId { get; set; }

        public ulong MessageId { get; set; }

        public ulong RoleId { get; set; }

        public IEmoji Emoji { get; set; }
    }
}