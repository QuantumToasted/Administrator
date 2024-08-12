namespace Administrator.Core;

public interface IBan : IRevocablePunishment
{
    int? MessagePruneDays { get; }
    
    DateTimeOffset? ExpiresAt { get; }
    
    PunishmentType IPunishment.Type => PunishmentType.Ban;
}