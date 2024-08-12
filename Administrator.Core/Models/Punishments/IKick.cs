namespace Administrator.Core;

public interface IKick : IPunishment
{
    PunishmentType IPunishment.Type => PunishmentType.Kick;
}