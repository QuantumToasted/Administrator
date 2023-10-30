using System.ComponentModel.DataAnnotations.Schema;
using Disqord;

namespace Administrator.Database;

public sealed record Block(
        Snowflake GuildId,
        Snowflake TargetId,
        string TargetName,
        Snowflake ModeratorId,
        string ModeratorName,
        string? Reason,
        [property: Column("channel")] Snowflake ChannelId,
        [property: Column("expires")] DateTimeOffset? ExpiresAt,
        [property: Column("previous_allow_permissions")] Permissions? PreviousChannelAllowPermissions,
        [property: Column("previous_deny_permissions")] Permissions? PreviousChannelDenyPermissions)
    : RevocablePunishment(GuildId, TargetId, TargetName, ModeratorId, ModeratorName, Reason), IExpiringDbEntity
{
    public override PunishmentType Type { get; init; } = PunishmentType.Block;
}