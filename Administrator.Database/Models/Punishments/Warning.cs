using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record Warning(Snowflake GuildId, UserSnapshot Target, UserSnapshot Moderator, string? Reason)
    : RevocablePunishment(GuildId, Target, Moderator, Reason), IStaticEntityTypeConfiguration<Warning>
{
    public int? AdditionalPunishmentId { get; set; }
    
    public Punishment? AdditionalPunishment { get; init; }
    
    //public override PunishmentType Type { get; init; } = PunishmentType.Warning;
    
    static void IStaticEntityTypeConfiguration<Warning>.ConfigureBuilder(EntityTypeBuilder<Warning> warning)
    {
        warning.HasBaseType<RevocablePunishment>();

        warning.HasPropertyWithColumnName(x => x.AdditionalPunishmentId, "additional_punishment");

        warning.HasOne(x => x.AdditionalPunishment);
    }
}