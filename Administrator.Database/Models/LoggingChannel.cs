using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record LoggingChannel : ILoggingChannel, IEntityTypeConfiguration<LoggingChannel>
{
    public Snowflake GuildId { get; init; }
    
    public Snowflake ChannelId { get; set; }
    
    public LogEventType EventType { get; init; }
    
    public GuildConfiguration? Guild { get; init; }

    public static LoggingChannel Create(IGuildChannel channel, LogEventType eventType) => Create(channel.GuildId, channel.Id, eventType);
    public static LoggingChannel Create(Snowflake guildId, Snowflake channelId, LogEventType eventType)
    {
        return new LoggingChannel
        {
            GuildId = guildId,
            ChannelId = channelId,
            EventType = eventType
        };
    }
    
    void IEntityTypeConfiguration<LoggingChannel>.Configure(EntityTypeBuilder<LoggingChannel> channel)
    {
        channel.HasKey(x => new { x.GuildId, x.EventType });
    }
}