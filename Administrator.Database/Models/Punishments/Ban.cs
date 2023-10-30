using System.ComponentModel.DataAnnotations.Schema;
using Disqord;

namespace Administrator.Database;

public sealed record Ban(
        Snowflake GuildId,
        Snowflake TargetId,
        string TargetName,
        Snowflake ModeratorId,
        string ModeratorName,
        string? Reason,
        [property: Column("message_prune_days")] int? MessagePruneDays,
        [property: Column("expires")] DateTimeOffset? ExpiresAt)
    : RevocablePunishment(GuildId, TargetId, TargetName, ModeratorId, ModeratorName, Reason), IExpiringDbEntity
{
    public override PunishmentType Type { get; init; } = PunishmentType.Ban;
}