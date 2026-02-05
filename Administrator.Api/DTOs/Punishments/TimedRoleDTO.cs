using System.Text.Json.Serialization;
using Administrator.Core;
using Disqord;

namespace Administrator.Api;

public sealed class TimedRoleDTO(ITimedRole timedRole) : RevocablePunishmentDTO(timedRole), ITimedRole
{
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public Snowflake RoleId => timedRole.RoleId;

    public TimedRoleApplyMode Mode => timedRole.Mode;

    public DateTimeOffset? ExpiresAt => timedRole.ExpiresAt;
}