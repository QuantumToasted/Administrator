using System.ComponentModel.DataAnnotations.Schema;

namespace Administrator.Database;

public sealed record Timeout(
        ulong GuildId, 
        ulong TargetId, 
        string TargetName, 
        ulong ModeratorId, 
        string ModeratorName, 
        string? Reason,
        [property: Column("expires")] DateTimeOffset ExpiresAt)
    : RevocablePunishment(GuildId, TargetId, TargetName, ModeratorId, ModeratorName, Reason)
{
    [Column("manually_revoked")]
    public bool WasManuallyRevoked { get; set; }
    
    public override PunishmentType Type { get; init; } = PunishmentType.Timeout;
}