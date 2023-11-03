using Administrator.Core;
using Administrator.Database;

namespace Administrator.Api;

public sealed record KickModel(
        int Id,
        UserSnapshot Target,
        UserSnapshot Moderator,
        DateTimeOffset CreatedAt,
        string? Reason)
    : PunishmentModel(Id, Target, Moderator, CreatedAt, Reason)
{
    public KickModel(Kick kick) 
        : this(kick.Id,
        kick.Target, 
        kick.Moderator,
        kick.CreatedAt, 
        kick.Reason)
    { }
}