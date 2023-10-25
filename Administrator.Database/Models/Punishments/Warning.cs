using System.ComponentModel.DataAnnotations.Schema;

namespace Administrator.Database;

public sealed record Warning(
        ulong GuildId, 
        ulong TargetId, 
        string TargetName, 
        ulong ModeratorId, 
        string ModeratorName, 
        string? Reason)
    : RevocablePunishment(GuildId, TargetId, TargetName, ModeratorId, ModeratorName, Reason)
{
    [Column("additional_punishment")]
    public int? AdditionalPunishmentId { get; set; }
    
    [ForeignKey(nameof(AdditionalPunishmentId))]
    public Punishment? AdditionalPunishment { get; init; }
    
    public override PunishmentType Type { get; init; } = PunishmentType.Warning;
}