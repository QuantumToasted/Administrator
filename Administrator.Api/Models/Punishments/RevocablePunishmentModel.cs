using System.Text.Json.Serialization;
using Administrator.Core;
using Administrator.Database;

namespace Administrator.Api;

public abstract record RevocablePunishmentModel(
            int Id, 
            UserSnapshot Target, 
            UserSnapshot Moderator, 
            DateTimeOffset CreatedAt, 
            string? Reason,
        [property: JsonPropertyName("revoked")]
            DateTimeOffset? RevokedAt, 
            UserSnapshot? Revoker, 
            string? RevocationReason, 
        [property: JsonPropertyName("appealed")]
            DateTimeOffset? AppealedAt, 
            string? AppealText, 
            AppealStatus? AppealStatus)
    : PunishmentModel(Id, Target, Moderator, CreatedAt, Reason);