using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record TimedRole : RevocablePunishment, ITimedRole, IEntityTypeConfiguration<TimedRole>
{
    public Snowflake RoleId { get; init; }
    
    public TimedRoleApplyMode Mode { get; init; }
    
    public DateTimeOffset? ExpiresAt { get; init; }

    public override PunishmentType Type => PunishmentType.TimedRole;

    void IEntityTypeConfiguration<TimedRole>.Configure(EntityTypeBuilder<TimedRole> timedRole)
    {
        timedRole.HasBaseType<RevocablePunishment>();
    }
}