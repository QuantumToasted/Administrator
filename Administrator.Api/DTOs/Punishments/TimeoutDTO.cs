using Administrator.Core;

namespace Administrator.Api;

public sealed class TimeoutDTO(ITimeout timeout) : RevocablePunishmentDTO(timeout), ITimeout
{
    public DateTimeOffset ExpiresAt => timeout.ExpiresAt;
}