using System.Diagnostics.CodeAnalysis;
using System.Text;
using Administrator.Core;
using Disqord;

namespace Administrator.Api;

public sealed class CreateTimeoutDTO : CreatePunishmentDTO
{
    public TimeSpan Duration { get; init; }
    
    public override bool Validate(Snowflake guildId, IDiscordEntityRequester requester, [NotNullWhen(false)] out string? error)
    {
        var baseResult = base.Validate(guildId, requester, out error);
        
        var errorBuilder = new StringBuilder();
        
        if (Duration <= TimeSpan.Zero)
            errorBuilder.AppendLine("\"duration\" is required.");
        
        if (Duration > TimeSpan.FromDays(28))
            errorBuilder.AppendLine("\"duration\" must be shorter than 28 days.");
        
        error = errorBuilder.Length > 0
            ? errorBuilder.ToString()
            : null;
        
        return baseResult || error is null;
    }
}