using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public enum TimedRoleApplyMode
{
    Grant = 1,
    Revoke
}

public sealed record TimedRole(Snowflake GuildId, UserSnapshot Target, UserSnapshot Moderator, string? Reason, 
        Snowflake RoleId, TimedRoleApplyMode Mode, DateTimeOffset? ExpiresAt)
    : RevocablePunishment(GuildId, Target, Moderator, Reason), IExpiringDbEntity, IStaticEntityTypeConfiguration<TimedRole>
{
    //public override PunishmentType Type { get; init; } = PunishmentType.TimedRole;
    
    static void IStaticEntityTypeConfiguration<TimedRole>.ConfigureBuilder(EntityTypeBuilder<TimedRole> timedRole)
    {
        timedRole.HasBaseType<RevocablePunishment>();

        timedRole.HasPropertyWithColumnName(x => x.RoleId, "role");
        timedRole.HasPropertyWithColumnName(x => x.Mode, "mode");
        timedRole.HasPropertyWithColumnName(x => x.ExpiresAt, "expires");
    }
}