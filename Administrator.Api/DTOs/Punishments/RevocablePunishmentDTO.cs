using Administrator.Core;

namespace Administrator.Api;

public abstract class RevocablePunishmentDTO(IRevocablePunishment punishment) : PunishmentDTO(punishment), IRevocablePunishment
{
    public DateTimeOffset? RevokedAt => punishment.RevokedAt;

    public UserSnapshot? Revoker => punishment.Revoker;

    public string? RevocationReason => punishment.RevocationReason;

    public DateTimeOffset? AppealedAt => punishment.AppealedAt;

    public string? AppealText => punishment.AppealText;
}

/*
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
    : PunishmentDTO(Id, Target, Moderator, CreatedAt, Reason);
*/