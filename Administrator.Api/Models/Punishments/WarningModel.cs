using System.Text.Json.Serialization;
using Administrator.Core;
using Administrator.Database;

namespace Administrator.Api;

public sealed record WarningModel(
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
        [property: JsonPropertyName("additionalPunishment")]
            int? AdditionalPunishmentId)
    : RevocablePunishmentModel(Id, Target, Moderator, CreatedAt, Reason, RevokedAt, Revoker, RevocationReason, AppealedAt, AppealText, AppealStatus)
{
    public WarningModel(Warning warning)
        : this(warning.Id,
            warning.Target,
            warning.Moderator,
            warning.CreatedAt, 
            warning.Reason,
            warning.RevokedAt, 
            warning.Revoker,
            warning.RevocationReason,
            warning.AppealedAt,
            warning.AppealText,
            warning.AppealStatus,
            warning.AdditionalPunishmentId)
    { }
}