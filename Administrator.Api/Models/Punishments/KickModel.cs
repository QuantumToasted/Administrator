using Administrator.Database;

namespace Administrator.Api;

public sealed record KickModel(
        int Id,
        UserModel Target,
        UserModel Moderator,
        DateTimeOffset CreatedAt,
        string? Reason)
    : PunishmentModel(Id, Target, Moderator, CreatedAt, Reason)
{
    public KickModel(Kick kick) 
        : this(kick.Id,
        new UserModel(kick.TargetId, kick.TargetName),
        new UserModel(kick.ModeratorId, kick.ModeratorName),
        kick.CreatedAt, 
        kick.Reason)
    { }
}