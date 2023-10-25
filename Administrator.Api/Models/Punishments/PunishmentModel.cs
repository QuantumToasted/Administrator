using System.Text.Json.Serialization;
using Administrator.Database;
using Timeout = Administrator.Database.Timeout;

namespace Administrator.Api;

public abstract record PunishmentModel(
        int Id, 
        UserModel Target, 
        UserModel Moderator, 
    [property: JsonPropertyName("created")]
        DateTimeOffset CreatedAt, 
        string? Reason)
{
    public static PunishmentModel FromPunishment(Punishment punishment)
    {
        return punishment switch
        {
            Kick kick => new KickModel(kick),
            Block block => new BlockModel(block),
            Ban ban => new BanModel(ban),
            TimedRole timedRole => new TimedRoleModel(timedRole),
            Timeout timeout => new TimeoutModel(timeout),
            Warning warning => new WarningModel(warning),
            _ => throw new ArgumentOutOfRangeException(nameof(punishment))
        };
    }
}