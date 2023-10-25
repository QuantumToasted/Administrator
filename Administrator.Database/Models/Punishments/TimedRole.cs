using System.ComponentModel.DataAnnotations.Schema;

namespace Administrator.Database;

public enum TimedRoleApplyMode
{
    Grant = 1,
    Revoke
}

public sealed record TimedRole(
        ulong GuildId,
        ulong TargetId,
        string TargetName,
        ulong ModeratorId,
        string ModeratorName,
        string? Reason,
        [property: Column("role")] ulong RoleId,
        [property: Column("expires")] DateTimeOffset? ExpiresAt,
        [property: Column("mode")] TimedRoleApplyMode Mode)
    : RevocablePunishment(GuildId, TargetId, TargetName, ModeratorId, ModeratorName, Reason)
{
    public override PunishmentType Type { get; init; } = PunishmentType.TimedRole;
}