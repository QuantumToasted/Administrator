using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record Ban(Snowflake GuildId, UserSnapshot Target, UserSnapshot Moderator, string? Reason, int? MessagePruneDays, DateTimeOffset? ExpiresAt)
    : RevocablePunishment(GuildId, Target, Moderator, Reason), IExpiringDbEntity, Core.IBan
{
    public override PunishmentType Type => PunishmentType.Ban;

    private sealed class BanConfiguration : IEntityTypeConfiguration<Ban>
    {
        public void Configure(EntityTypeBuilder<Ban> ban)
        {
            ban.HasBaseType<RevocablePunishment>();
        }
    }
}