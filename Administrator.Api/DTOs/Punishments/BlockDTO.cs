using System.Text.Json.Serialization;
using Administrator.Core;
using Disqord;

namespace Administrator.Api;

public sealed class BlockDTO(IBlock block) : RevocablePunishmentDTO(block), IBlock
{
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public Snowflake ChannelId => block.ChannelId;

    public DateTimeOffset? ExpiresAt => block.ExpiresAt;
}