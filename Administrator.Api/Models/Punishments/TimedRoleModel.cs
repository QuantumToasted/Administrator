using System.Text.Json.Serialization;
using Administrator.Database;

namespace Administrator.Api;

public sealed record TimedRoleModel(
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
        [property: JsonPropertyName("role")]
            ulong RoleId, 
        [property: JsonPropertyName("expires")]
            DateTimeOffset? ExpiresAt, 
            TimedRoleApplyMode Mode)
    : RevocablePunishmentModel(Id, Target, Moderator, CreatedAt, Reason, RevokedAt, Revoker, RevocationReason, AppealedAt, AppealText, AppealStatus)
{
    public TimedRoleModel(TimedRole timedRole)
        : this(timedRole.Id,
            new UserModel(timedRole.TargetId, timedRole.TargetName),
            new UserModel(timedRole.ModeratorId, timedRole.ModeratorName),
            timedRole.CreatedAt, 
            timedRole.Reason,
            timedRole.RevokedAt, 
            timedRole.RevokerId.HasValue 
                ? new UserModel(timedRole.RevokerId.Value, timedRole.RevokerName!)
                : null,
            timedRole.RevocationReason,
            timedRole.AppealedAt,
            timedRole.AppealText,
            timedRole.AppealStatus,
            timedRole.RoleId,
            timedRole.ExpiresAt,
            timedRole.Mode)
    { }
}