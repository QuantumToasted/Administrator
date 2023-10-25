using System.Text.Json.Serialization;
using Administrator.Database;

namespace Administrator.Api;

public sealed record WarningModel(
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
        [property: JsonPropertyName("additionalPunishment")]
            int? AdditionalPunishmentId)
    : RevocablePunishmentModel(Id, Target, Moderator, CreatedAt, Reason, RevokedAt, Revoker, RevocationReason, AppealedAt, AppealText, AppealStatus)
{
    public WarningModel(Warning warning)
        : this(warning.Id,
            new UserModel(warning.TargetId, warning.TargetName),
            new UserModel(warning.ModeratorId, warning.ModeratorName),
            warning.CreatedAt, 
            warning.Reason,
            warning.RevokedAt, 
            warning.RevokerId.HasValue 
                ? new UserModel(warning.RevokerId.Value, warning.RevokerName!)
                : null,
            warning.RevocationReason,
            warning.AppealedAt,
            warning.AppealText,
            warning.AppealStatus,
            warning.AdditionalPunishmentId)
    { }
}