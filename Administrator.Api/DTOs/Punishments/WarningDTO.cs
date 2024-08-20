using Administrator.Core;

namespace Administrator.Api;

public sealed class WarningDTO(IWarning warning) : RevocablePunishmentDTO(warning), IWarning
{
    public int DemeritPoints => warning.DemeritPoints;
    
    public int DemeritPointsRemaining => warning.DemeritPointsRemaining;

    public int? AdditionalPunishmentId => warning.AdditionalPunishmentId;
}