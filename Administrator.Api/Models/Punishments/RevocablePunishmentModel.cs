using System.Text.Json.Serialization;
using Administrator.Database;

namespace Administrator.Api;

public abstract record RevocablePunishmentModel(
            int Id, 
            UserModel Target, 
            UserModel Moderator, 
            DateTimeOffset CreatedAt, 
            string? Reason,
        [property: JsonPropertyName("revoked")]
            DateTimeOffset? RevokedAt, 
            UserModel? Revoker, 
            string? RevocationReason, 
        [property: JsonPropertyName("appealed")]
            DateTimeOffset? AppealedAt, 
            string? AppealText, 
            AppealStatus? AppealStatus)
    : PunishmentModel(Id, Target, Moderator, CreatedAt, Reason);