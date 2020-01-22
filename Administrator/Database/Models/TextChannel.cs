using Administrator.Common;

namespace Administrator.Database
{
    public sealed class TextChannel
    {
        private TextChannel()
        { }

        public TextChannel(ulong guildId, ulong channelId)
        {
            GuildId = guildId;
            ChannelId = channelId;
        }

        public ulong GuildId { get; set; }

        public ulong ChannelId { get; set; }

        public TextChannelSettings Settings { get; set; } = TextChannelSettings.SendCommandErrors;
    }
}