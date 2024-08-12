using System.Diagnostics.CodeAnalysis;
using System.Text;
using Administrator.Core;
using Disqord;

namespace Administrator.Api;

public sealed class CreateWarningDTO : CreatePunishmentDTO
{
    public int? DemeritPoints { get; init; }

    public override bool Validate(Snowflake guildId, IDiscordEntityRequester requester, [NotNullWhen(false)] out string? error)
    {
        var baseResult = base.Validate(guildId, requester, out error);
        
        var errorBuilder = new StringBuilder();
        
        if (DemeritPoints is < 0 or > 50)
            errorBuilder.AppendLine("Cannot apply more than 50 or less than 0 demerit points.");
        
        error = errorBuilder.Length > 0
            ? errorBuilder.ToString()
            : null;
        
        return baseResult || error is null;
    }
}