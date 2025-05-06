using Administrator.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed class Timeout : RevocablePunishment, ITimeout, IEntityTypeConfiguration<Timeout>
{
    public DateTimeOffset ExpiresAt { get; init; }
    
    public bool WasManuallyRevoked { get; set; }

    public override PunishmentType Type => PunishmentType.Timeout;

    void IEntityTypeConfiguration<Timeout>.Configure(EntityTypeBuilder<Timeout> timeout)
    {
        timeout.HasBaseType<RevocablePunishment>();
    }
}