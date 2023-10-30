using System.ComponentModel.DataAnnotations.Schema;
using Disqord;

namespace Administrator.Database;

public enum TimedRoleApplyMode
{
    Grant = 1,
    Revoke
}

public sealed record TimedRole(
        Snowflake GuildId,
        Snowflake TargetId,
        string TargetName,
        Snowflake ModeratorId,
        string ModeratorName,
        string? Reason,
        [property: Column("role")] Snowflake RoleId,
        [property: Column("mode")] TimedRoleApplyMode Mode,
        [property: Column("expires")] DateTimeOffset? ExpiresAt)
    : RevocablePunishment(GuildId, TargetId, TargetName, ModeratorId, ModeratorName, Reason), IExpiringDbEntity
{
    public override PunishmentType Type { get; init; } = PunishmentType.TimedRole;
}