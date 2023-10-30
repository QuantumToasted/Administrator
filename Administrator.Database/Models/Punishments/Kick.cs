using Disqord;

namespace Administrator.Database;

public sealed record Kick(
        Snowflake GuildId,
        Snowflake TargetId,
        string TargetName,
        Snowflake ModeratorId,
        string ModeratorName,
        string? Reason)
    : Punishment(GuildId, TargetId, TargetName, ModeratorId, ModeratorName, Reason)
{
    public override PunishmentType Type { get; init; } = PunishmentType.Kick;
}