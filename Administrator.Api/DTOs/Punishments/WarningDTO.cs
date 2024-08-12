using Administrator.Core;

namespace Administrator.Api;

public sealed class WarningDTO(IWarning warning) : PunishmentDTO(warning), IWarning
{
    public int DemeritPoints => warning.DemeritPoints;

    public int? AdditionalPunishmentId => warning.AdditionalPunishmentId;
}