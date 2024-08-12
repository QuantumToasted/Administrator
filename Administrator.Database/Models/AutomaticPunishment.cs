using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public record AutomaticPunishment(Snowflake GuildId, int DemeritPoints, PunishmentType PunishmentType, TimeSpan? PunishmentDuration)
{
    public PunishmentType PunishmentType { get; set; } = PunishmentType;

    public TimeSpan? PunishmentDuration { get; set; } = PunishmentDuration;
    
    public Guild? Guild { get; set; }

    private sealed class AutomaticPunishmentConfiguration : IEntityTypeConfiguration<AutomaticPunishment>
    {
        public void Configure(EntityTypeBuilder<AutomaticPunishment> autoPunishment)
        {
            autoPunishment.HasKey(x => new { x.GuildId, x.DemeritPoints });
        }
    }
}