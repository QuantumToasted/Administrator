using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record Timeout(Snowflake GuildId, UserSnapshot Target, UserSnapshot Moderator, string? Reason, 
        DateTimeOffset ExpiresAt)
    : RevocablePunishment(GuildId, Target, Moderator, Reason), IExpiringDbEntity, IStaticEntityTypeConfiguration<Timeout>
{
    public bool WasManuallyRevoked { get; set; }
    
    //public override PunishmentType Type { get; init; } = PunishmentType.Timeout;

    DateTimeOffset? IExpiringDbEntity.ExpiresAt => ExpiresAt;
    public static void ConfigureBuilder(EntityTypeBuilder<Timeout> timeout)
    {
        timeout.HasBaseType<RevocablePunishment>();

        timeout.HasPropertyWithColumnName(x => x.ExpiresAt, "expires");
        timeout.HasPropertyWithColumnName(x => x.WasManuallyRevoked, "manually_revoked");
    }
}