using System.ComponentModel.DataAnnotations.Schema;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

[Table("warning_punishments")]
public sealed record WarningPunishment(Snowflake GuildId, int WarningCount, PunishmentType PunishmentType, TimeSpan? PunishmentDuration) 
    : IStaticEntityTypeConfiguration<WarningPunishment>
{
    public PunishmentType PunishmentType { get; set; } = PunishmentType;

    public TimeSpan? PunishmentDuration { get; set; } = PunishmentDuration;
    
    public Guild? Guild { get; init; }

    // Follow this example
    static void IStaticEntityTypeConfiguration<WarningPunishment>.ConfigureBuilder(EntityTypeBuilder<WarningPunishment> warningPunishment)
    {
        // Table
        warningPunishment.ToTable("warning_punishments");
        warningPunishment.HasKey(x => new { x.GuildId, x.WarningCount });

        
        // Properties
        warningPunishment.HasPropertyWithColumnName(x => x.GuildId, "guild");
        warningPunishment.HasPropertyWithColumnName(x => x.WarningCount, "warnings");
        warningPunishment.HasPropertyWithColumnName(x => x.PunishmentType, "type");
        warningPunishment.HasPropertyWithColumnName(x => x.PunishmentDuration, "duration");
        
        // Relations
        // Extras
    }
}