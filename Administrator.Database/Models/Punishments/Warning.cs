using Administrator.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record Warning : RevocablePunishment, IWarning, IEntityTypeConfiguration<Warning>
{
    public int DemeritPoints { get; init; }
    
    public int DemeritPointsRemaining { get; set; }
    
    public int? AdditionalPunishmentId { get; set; }
    
    public Punishment? AdditionalPunishment { get; init; }

    public override PunishmentType Type => PunishmentType.Warning;

    void IEntityTypeConfiguration<Warning>.Configure(EntityTypeBuilder<Warning> warning)
    {
        warning.HasBaseType<RevocablePunishment>();
            
        warning.HasOne(x => x.AdditionalPunishment);
    }
}