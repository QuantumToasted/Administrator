using System.Text.Json.Serialization;
using Administrator.Core;
using Administrator.Database;

namespace Administrator.Api;

public sealed record BanModel(
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
            int? MessagePruneDays,
        [property: JsonPropertyName("expires")]
            DateTimeOffset? ExpiresAt)
    : RevocablePunishmentModel(Id, Target, Moderator, CreatedAt, Reason, RevokedAt, Revoker, RevocationReason, AppealedAt, AppealText, AppealStatus)
{
    public BanModel(Ban ban)
        : this(ban.Id,
            ban.Target,
            ban.Moderator,
            ban.CreatedAt, 
            ban.Reason,
            ban.RevokedAt, 
            ban.Revoker,
            ban.RevocationReason,
            ban.AppealedAt,
            ban.AppealText,
            ban.AppealStatus,
            ban.MessagePruneDays, 
            ban.ExpiresAt)
    { }
}