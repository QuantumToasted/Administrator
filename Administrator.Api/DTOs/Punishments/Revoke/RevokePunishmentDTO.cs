using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Serialization;
using Administrator.Core;
using Disqord;

namespace Administrator.Api;

public sealed class RevokePunishmentDTO
{
    public string? Reason { get; init; }
    
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public Snowflake RevokerId { get; init; }
    
    public bool Validate(Snowflake guildId, IDiscordEntityRequester requester, [NotNullWhen(false)] out string? error)
    {
        var errorBuilder = new StringBuilder();
        
        const int maxLength = Discord.Limits.Message.Embed.Field.MaxValueLength;
        if (Reason?.Length > maxLength)
            errorBuilder.AppendLine($"\"reason\" cannot be more than {maxLength} characters.");
        
        if (RevokerId == default)
            errorBuilder.AppendLine("\"revokerId\" is required.");

        if (requester.GetMember(guildId, RevokerId) is null)
            errorBuilder.AppendLine($"Could not find revoker with ID {RevokerId} in guild {guildId}.");
        
        error = errorBuilder.Length > 0
            ? errorBuilder.ToString()
            : null;
        
        return error is null;
    }
}