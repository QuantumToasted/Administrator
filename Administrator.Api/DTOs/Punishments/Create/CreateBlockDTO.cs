using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Serialization;
using Administrator.Core;
using Disqord;

namespace Administrator.Api;

public sealed class CreateBlockDTO : CreatePunishmentDTO
{
    public TimeSpan? Duration { get; init; }
    
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public Snowflake ChannelId { get; init; }
    
    public override bool Validate(Snowflake guildId, IDiscordEntityRequester requester, [NotNullWhen(false)] out string? error)
    {
        var baseResult = base.Validate(guildId, requester, out error);

        var errorBuilder = new StringBuilder();
        
        if (Duration < TimeSpan.Zero)
            errorBuilder.AppendLine("Cannot block for a negative duration of time.");

        if (ChannelId == default)
            errorBuilder.AppendLine("\"channelId\" is required.");

        if (requester.GetChannel(guildId, ChannelId) is not IThreadChannel or ITextChannel)
            errorBuilder.AppendLine($"Could not find thread or text channel with ID {TargetId}.");

        error = errorBuilder.Length > 0
            ? errorBuilder.ToString()
            : null;
        
        return baseResult || error is null;
    }
}