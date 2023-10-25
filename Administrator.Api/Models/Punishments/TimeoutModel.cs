using System.Text.Json.Serialization;
using Administrator.Database;
using Timeout = Administrator.Database.Timeout;

namespace Administrator.Api;

public sealed record TimeoutModel(
            int Id, 
            UserModel Target, 
            UserModel Moderator, 
            DateTimeOffset CreatedAt, 
            string? Reason, 
            DateTimeOffset? RevokedAt, 
            UserModel? Revoker, 
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
            new UserModel(timeout.TargetId, timeout.TargetName),
            new UserModel(timeout.ModeratorId, timeout.ModeratorName),
            timeout.CreatedAt, 
            timeout.Reason,
            timeout.RevokedAt, 
            timeout.RevokerId.HasValue 
                ? new UserModel(timeout.RevokerId.Value, timeout.RevokerName!)
                : null,
            timeout.RevocationReason,
            timeout.AppealedAt,
            timeout.AppealText,
            timeout.AppealStatus,
            timeout.ExpiresAt)
    { }
}