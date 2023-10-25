using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Administrator.Database;

[Table("warning_punishments")]
[PrimaryKey(nameof(GuildId), nameof(WarningCount))]
public sealed record WarningPunishment(
    [property: Column("guild")] ulong GuildId, 
    [property: Column("warnings")] int WarningCount,
    PunishmentType PunishmentType,
    TimeSpan? PunishmentDuration)
{
    [Column("type")] 
    public PunishmentType PunishmentType { get; set; } = PunishmentType;

    [Column("duration")] 
    public TimeSpan? PunishmentDuration { get; set; } = PunishmentDuration;
    
    [ForeignKey(nameof(GuildId))]
    public Guild? Guild { get; init; }
}