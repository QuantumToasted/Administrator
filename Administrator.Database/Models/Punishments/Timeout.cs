using System.ComponentModel.DataAnnotations.Schema;
using Disqord;

namespace Administrator.Database;

public sealed record Timeout(
        Snowflake GuildId, 
        Snowflake TargetId, 
        string TargetName, 
        Snowflake ModeratorId, 
        string ModeratorName, 
        string? Reason,
        [property: Column("expires")] DateTimeOffset ExpiresAt)
    : RevocablePunishment(GuildId, TargetId, TargetName, ModeratorId, ModeratorName, Reason), IExpiringDbEntity
{
    [Column("manually_revoked")]
    public bool WasManuallyRevoked { get; set; }
    
    public override PunishmentType Type { get; init; } = PunishmentType.Timeout;

    DateTimeOffset? IExpiringDbEntity.ExpiresAt => ExpiresAt;
}