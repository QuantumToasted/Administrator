using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record Warning(Snowflake GuildId, UserSnapshot Target, UserSnapshot Moderator, string? Reason, int DemeritPoints, int DemeritPointSnapshot/*, bool DecayDemeritPoints*/)
    : Punishment(GuildId, Target, Moderator, Reason), IWarning
{
    public int? AdditionalPunishmentId { get; set; }
    
    public Punishment? AdditionalPunishment { get; init; }
    
    public override PunishmentType Type => PunishmentType.Warning;

    private sealed class WarningConfiguration : IEntityTypeConfiguration<Warning>
    {
        public void Configure(EntityTypeBuilder<Warning> warning)
        {
            warning.HasBaseType<Punishment>();
            
            warning.HasOne(x => x.AdditionalPunishment);
        }
    }
}