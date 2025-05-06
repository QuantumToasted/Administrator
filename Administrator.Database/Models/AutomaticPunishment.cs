using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed class AutomaticPunishment : IAutomaticPunishment, IEntityTypeConfiguration<AutomaticPunishment>
{
    public Snowflake GuildId { get; init; }
    
    public int DemeritPoints { get; init; }
    
    public PunishmentType PunishmentType { get; set; }
    
    public TimeSpan? PunishmentDuration { get; set; }
    
    public GuildConfiguration? Guild { get; init; }

    public static AutomaticPunishment Ban(Snowflake guildId, int demeritPoints, TimeSpan? duration) => Create(guildId, demeritPoints, PunishmentType.Ban, duration);
    public static AutomaticPunishment Kick(Snowflake guildId, int demeritPoints) => Create(guildId, demeritPoints, PunishmentType.Kick, null);
    public static AutomaticPunishment Timeout(Snowflake guildId, int demeritPoints, TimeSpan duration) => Create(guildId, demeritPoints, PunishmentType.Timeout, duration);
    public static AutomaticPunishment Create(Snowflake guildId, int demeritPoints, PunishmentType type, TimeSpan? duration)
    {
        return new AutomaticPunishment
        {
            GuildId = guildId,
            DemeritPoints = demeritPoints,
            PunishmentType = type,
            PunishmentDuration = duration
        };
    }

    void IEntityTypeConfiguration<AutomaticPunishment>.Configure(EntityTypeBuilder<AutomaticPunishment> autoPunishment)
    {
        autoPunishment.HasKey(x => new { x.GuildId, x.DemeritPoints });
    }
}