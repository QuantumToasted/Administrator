namespace Administrator.Core;

public interface ITimeout : IRevocablePunishment
{
    DateTimeOffset ExpiresAt { get; }
    
    PunishmentType IPunishment.Type => PunishmentType.Timeout;
}