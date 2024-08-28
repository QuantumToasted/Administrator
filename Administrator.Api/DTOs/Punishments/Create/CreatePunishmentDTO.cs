using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Serialization;
using Administrator.Core;
using Disqord;

namespace Administrator.Api;

public abstract class CreatePunishmentDTO
{
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public Snowflake ModeratorId { get; init; }
    
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public Snowflake TargetId { get; init; }
    
    public string? Reason { get; init; }

    public virtual bool Validate(Snowflake guildId, IDiscordEntityRequester requester, [NotNullWhen(false)] out string? error)
    {
        var errorBuilder = new StringBuilder();

        const int maxLength = Discord.Limits.Message.Embed.Field.MaxValueLength;
        if (Reason?.Length > maxLength)
            errorBuilder.AppendLine($"\"reason\" cannot be more than {maxLength} characters.");

        if (TargetId == default)
            errorBuilder.AppendLine("\"targetId\" is required.");

        if (requester.GetUser(TargetId) is null)
            errorBuilder.AppendLine($"Could not find target with ID {TargetId}.");

        if (ModeratorId == default)
            errorBuilder.AppendLine("\"moderatorId\" is required.");

        if (requester.GetMember(guildId, ModeratorId) is null)
            errorBuilder.AppendLine($"Could not find moderator with ID {ModeratorId} in guild {guildId}.");

        error = errorBuilder.Length > 0
            ? errorBuilder.ToString()
            : null;
        
        return error is null;
    }
}