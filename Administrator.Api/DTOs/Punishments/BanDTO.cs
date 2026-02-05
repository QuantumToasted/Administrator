using Administrator.Core;

namespace Administrator.Api;

public sealed class BanDTO(IBan ban) : RevocablePunishmentDTO(ban), IBan
{
    public int? MessagePruneDays => ban.MessagePruneDays;

    public DateTimeOffset? ExpiresAt => ban.ExpiresAt;
}