using System.ComponentModel.DataAnnotations.Schema;

namespace Administrator.Database;

public sealed record Ban(
        ulong GuildId,
        ulong TargetId,
        string TargetName,
        ulong ModeratorId,
        string ModeratorName,
        string? Reason,
        [property: Column("message_prune_days")] int? MessagePruneDays,
        [property: Column("expires")] DateTimeOffset? ExpiresAt)
    : RevocablePunishment(GuildId, TargetId, TargetName, ModeratorId, ModeratorName, Reason)
{
    public override PunishmentType Type { get; init; } = PunishmentType.Ban;
}