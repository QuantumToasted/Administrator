using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Serialization;
using Administrator.Core;
using Disqord;

namespace Administrator.Api;

public sealed class CreateTimedRoleDTO : CreatePunishmentDTO
{
    public TimeSpan? Duration { get; init; }
    
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public Snowflake RoleId { get; init; }
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TimedRoleApplyMode Mode { get; init; }
    
    public override bool Validate(Snowflake guildId, IDiscordEntityRequester requester, [NotNullWhen(false)] out string? error)
    {
        var baseResult = base.Validate(guildId, requester, out error);

        var errorBuilder = new StringBuilder();
        
        if (Duration < TimeSpan.Zero)
            errorBuilder.AppendLine("Cannot ban for a negative duration of time.");

        if (RoleId == default)
            errorBuilder.AppendLine("\"roleId\" is required.");

        if (requester.GetRole(guildId, RoleId) is null)
            errorBuilder.AppendLine($"Could not find role with ID {TargetId}.");

        error = errorBuilder.Length > 0
            ? errorBuilder.ToString()
            : null;
        
        return baseResult || error is null;
    }
}