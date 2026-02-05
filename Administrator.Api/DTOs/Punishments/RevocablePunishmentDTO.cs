using Administrator.Core;

namespace Administrator.Api;

public abstract class RevocablePunishmentDTO(IRevocablePunishment punishment) : PunishmentDTO(punishment), IRevocablePunishment
{
    public DateTimeOffset? RevokedAt => punishment.RevokedAt;

    public UserSnapshot? Revoker => punishment.Revoker;

    public string? RevocationReason => punishment.RevocationReason;

    public DateTimeOffset? AppealedAt => punishment.AppealedAt;

    public string? AppealText => punishment.AppealText;

    public AppealStatus? AppealStatus => punishment.AppealStatus;
}