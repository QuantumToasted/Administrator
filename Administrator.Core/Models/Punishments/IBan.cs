namespace Administrator.Core;

public interface IBan : IRevocablePunishment, IExpiringEntity
{
    int? MessagePruneDays { get; }
    
    PunishmentType IPunishment.Type => PunishmentType.Ban;
}