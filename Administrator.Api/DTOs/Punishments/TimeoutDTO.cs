using Administrator.Core;

namespace Administrator.Api;

public sealed class TimeoutDTO(ITimeout timeout) : RevocablePunishmentDTO(timeout), ITimeout
{
    public DateTimeOffset ExpiresAt => timeout.ExpiresAt;
}

/*
public sealed record TimeoutModel(
            int Id, 
            UserSnapshot Target, 
            UserSnapshot Moderator, 
            DateTimeOffset CreatedAt, 
            string? Reason, 
            DateTimeOffset? RevokedAt, 
            UserSnapshot? Revoker, 
            string? RevocationReason, 
            DateTimeOffset? AppealedAt, 
            string? AppealText, 
            AppealStatus? AppealStatus,
        [property: JsonPropertyName("expires")]
            DateTimeOffset ExpiresAt)
    : RevocablePunishmentModel(Id, Target, Moderator, CreatedAt, Reason, RevokedAt, Revoker, RevocationReason, AppealedAt, AppealText, AppealStatus)
{
    public TimeoutModel(Timeout timeout)
        : this(timeout.Id,
            timeout.Target, 
            timeout.Moderator,
            timeout.CreatedAt, 
            timeout.Reason,
            timeout.RevokedAt, 
            timeout.Revoker,
            timeout.RevocationReason,
            timeout.AppealedAt,
            timeout.AppealText,
            timeout.AppealStatus,
            timeout.ExpiresAt)
    { }
}
*/