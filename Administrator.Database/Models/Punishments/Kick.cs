using Administrator.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed class Kick : Punishment, IKick, IEntityTypeConfiguration<Kick>
{
    public override PunishmentType Type => PunishmentType.Kick;
    
    void IEntityTypeConfiguration<Kick>.Configure(EntityTypeBuilder<Kick> kick)
    {
        kick.HasBaseType<Punishment>();
    }
}