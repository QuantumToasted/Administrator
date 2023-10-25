using System.Text.Json.Serialization;
using Administrator.Database;

namespace Administrator.Api;

public sealed record BanModel(
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
            int? MessagePruneDays,
        [property: JsonPropertyName("expires")]
            DateTimeOffset? ExpiresAt)
    : RevocablePunishmentModel(Id, Target, Moderator, CreatedAt, Reason, RevokedAt, Revoker, RevocationReason, AppealedAt, AppealText, AppealStatus)
{
    public BanModel(Ban ban)
        : this(ban.Id,
            new UserModel(ban.TargetId, ban.TargetName),
            new UserModel(ban.ModeratorId, ban.ModeratorName),
            ban.CreatedAt, 
            ban.Reason,
            ban.RevokedAt, 
            ban.RevokerId.HasValue 
                ? new UserModel(ban.RevokerId.Value, ban.RevokerName!)
                : null,
            ban.RevocationReason,
            ban.AppealedAt,
            ban.AppealText,
            ban.AppealStatus,
            ban.MessagePruneDays, 
            ban.ExpiresAt)
    { }
}