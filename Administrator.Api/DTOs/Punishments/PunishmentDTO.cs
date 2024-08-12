using System.Text.Json.Serialization;
using Administrator.Core;
using Disqord;

namespace Administrator.Api;

public abstract class PunishmentDTO(IPunishment punishment) : IPunishment
{
    public int Id => punishment.Id;

    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public Snowflake GuildId => punishment.GuildId;

    public UserSnapshot Target => punishment.Target;

    public UserSnapshot Moderator => punishment.Moderator;

    public DateTimeOffset CreatedAt => punishment.CreatedAt;

    public string? Reason => punishment.Reason;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PunishmentType Type => punishment.Type;

    public static PunishmentDTO FromPunishment(IPunishment punishment)
    {
        return punishment switch
        {
            Core.IBan ban => new BanDTO(ban),
            IBlock block => new BlockDTO(block),
            IKick kick => new KickDTO(kick),
            ITimedRole timedRole => new TimedRoleDTO(timedRole),
            ITimeout timeout => new TimeoutDTO(timeout),
            IWarning warning => new WarningDTO(warning),
            _ => throw new ArgumentOutOfRangeException(nameof(punishment))
        };
    }
}