using System.Diagnostics.CodeAnalysis;
using System.Text;
using Administrator.Core;
using Disqord;

namespace Administrator.Api;

public sealed class CreateBanDTO : CreatePunishmentDTO
{
    public TimeSpan? Duration { get; init; }
    
    public int? MessagePruneDays { get; init; }

    public override bool Validate(Snowflake guildId, IDiscordEntityRequester requester, [NotNullWhen(false)] out string? error)
    {
        var baseResult = base.Validate(guildId, requester, out error);
        
        var errorBuilder = new StringBuilder();
        
        if (Duration < TimeSpan.Zero)
            errorBuilder.AppendLine("Cannot ban for a negative duration of time.");

        if (MessagePruneDays is < 0 or > 7)
            errorBuilder.AppendLine("Cannot prune more than 7 or less than 0 days' worth of messages.");
        
        error = errorBuilder.Length > 0
            ? errorBuilder.ToString()
            : null;
        
        return baseResult || error is null;
    }
}