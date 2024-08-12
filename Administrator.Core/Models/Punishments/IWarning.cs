namespace Administrator.Core;

public interface IWarning : IPunishment
{
    int DemeritPoints { get; }
    
    int? AdditionalPunishmentId { get; }
    
    PunishmentType IPunishment.Type => PunishmentType.Warning;
}