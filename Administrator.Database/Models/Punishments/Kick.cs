namespace Administrator.Database;

public sealed record Kick(
        ulong GuildId,
        ulong TargetId,
        string TargetName,
        ulong ModeratorId,
        string ModeratorName,
        string? Reason)
    : Punishment(GuildId, TargetId, TargetName, ModeratorId, ModeratorName, Reason)
{
    public override PunishmentType Type { get; init; } = PunishmentType.Kick;
}