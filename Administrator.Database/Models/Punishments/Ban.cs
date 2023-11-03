using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record Ban(Snowflake GuildId, UserSnapshot Target, UserSnapshot Moderator, string? Reason,
        int? MessagePruneDays, DateTimeOffset? ExpiresAt)
    : RevocablePunishment(GuildId, Target, Moderator, Reason), IExpiringDbEntity, IStaticEntityTypeConfiguration<Ban>
{
    //public override PunishmentType Type { get; init; } = PunishmentType.Ban;
    
    static void IStaticEntityTypeConfiguration<Ban>.ConfigureBuilder(EntityTypeBuilder<Ban> ban)
    {
        ban.HasBaseType<RevocablePunishment>();

        ban.HasPropertyWithColumnName(x => x.MessagePruneDays, "prune_days");
        ban.HasPropertyWithColumnName(x => x.ExpiresAt, "expires");
    }
}