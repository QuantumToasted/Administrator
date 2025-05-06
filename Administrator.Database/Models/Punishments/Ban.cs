using Administrator.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed class Ban : RevocablePunishment, Core.IBan, IEntityTypeConfiguration<Ban>
{
    public int? MessagePruneDays { get; init; }
    
    public DateTimeOffset? ExpiresAt { get; init; }
    
    public override PunishmentType Type => PunishmentType.Ban;

    void IEntityTypeConfiguration<Ban>.Configure(EntityTypeBuilder<Ban> ban)
    {
        ban.HasBaseType<RevocablePunishment>();
    }
}