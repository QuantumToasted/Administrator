namespace Administrator.Core;

public interface ITimeout : IRevocablePunishment, IExpiringEntity
{
    new DateTimeOffset ExpiresAt { get; }
    
    PunishmentType IPunishment.Type => PunishmentType.Timeout;
    DateTimeOffset? IExpiringEntity.ExpiresAt => ExpiresAt;
}