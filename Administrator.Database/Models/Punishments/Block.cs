using System.ComponentModel.DataAnnotations.Schema;

namespace Administrator.Database;

public sealed record Block(
        ulong GuildId,
        ulong TargetId,
        string TargetName,
        ulong ModeratorId,
        string ModeratorName,
        string? Reason,
        [property: Column("channel")] ulong ChannelId,
        [property: Column("expires")] DateTimeOffset? ExpiresAt,
        [property: Column("previous_allow_permissions")] ulong? PreviousChannelAllowPermissions,
        [property: Column("previous_deny_permissions")] ulong? PreviousChannelDenyPermissions)
    : RevocablePunishment(GuildId, TargetId, TargetName, ModeratorId, ModeratorName, Reason)
{
    public override PunishmentType Type { get; init; } = PunishmentType.Block;
}