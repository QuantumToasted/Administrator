using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record Warning(Snowflake GuildId, UserSnapshot Target, UserSnapshot Moderator, string? Reason, int DemeritPoints)
    : RevocablePunishment(GuildId, Target, Moderator, Reason), IWarning
{
    public int DemeritPointsRemaining { get; set; } = DemeritPoints;
    
    public int? AdditionalPunishmentId { get; set; }
    
    public Punishment? AdditionalPunishment { get; init; }
    
    public override PunishmentType Type => PunishmentType.Warning;

    private sealed class WarningConfiguration : IEntityTypeConfiguration<Warning>
    {
        public void Configure(EntityTypeBuilder<Warning> warning)
        {
            warning.HasBaseType<RevocablePunishment>();
            
            warning.HasOne(x => x.AdditionalPunishment);
        }
    }
}