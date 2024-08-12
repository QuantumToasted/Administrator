using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record TimedRole(Snowflake GuildId, UserSnapshot Target, UserSnapshot Moderator, string? Reason, Snowflake RoleId, TimedRoleApplyMode Mode, DateTimeOffset? ExpiresAt)
    : RevocablePunishment(GuildId, Target, Moderator, Reason), IExpiringDbEntity, ITimedRole
{
    public override PunishmentType Type => PunishmentType.TimedRole;

    private sealed class TimedRoleConfiguration : IEntityTypeConfiguration<TimedRole>
    {
        public void Configure(EntityTypeBuilder<TimedRole> timedRole)
        {
            timedRole.HasBaseType<RevocablePunishment>();
        }
    }
}