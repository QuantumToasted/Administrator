using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record Timeout(Snowflake GuildId, UserSnapshot Target, UserSnapshot Moderator, string? Reason, DateTimeOffset ExpiresAt)
    : RevocablePunishment(GuildId, Target, Moderator, Reason), IExpiringDbEntity, ITimeout
{
    public bool WasManuallyRevoked { get; set; }
    
    public override PunishmentType Type => PunishmentType.Timeout;

    DateTimeOffset? IExpiringDbEntity.ExpiresAt => ExpiresAt;

    private sealed class TimeoutConfiguration : IEntityTypeConfiguration<Timeout>
    {
        public void Configure(EntityTypeBuilder<Timeout> timeout)
        {
            timeout.HasBaseType<RevocablePunishment>();
        }
    }
}