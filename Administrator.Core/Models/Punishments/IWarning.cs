namespace Administrator.Core;

public interface IWarning : IRevocablePunishment
{
    int DemeritPoints { get; }
    
    int DemeritPointsRemaining { get; }
    
    int? AdditionalPunishmentId { get; }
    
    PunishmentType IPunishment.Type => PunishmentType.Warning;
}