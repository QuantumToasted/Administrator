using Disqord;

namespace Administrator.Core;

public interface IAutomaticPunishment : IGuildEntity
{
    int DemeritPoints { get; }
    
    PunishmentType PunishmentType { get; }
    
    TimeSpan? PunishmentDuration { get; }
}