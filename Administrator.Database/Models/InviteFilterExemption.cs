using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public enum InviteFilterExemptionType
{
    Guild = 1, // invites from TargetId (guild)
    Channel, // invites posted in TargetId (channel and channel's threads)
    Role, // invites posted from members with TargetId (role)
    User, // invites posted from TargetId (user)
    InviteCode // invites with the code InviteCode
}

public sealed record InviteFilterExemption(Snowflake GuildId, InviteFilterExemptionType ExemptionType, Snowflake? TargetId, string? InviteCode) 
    : IStaticEntityTypeConfiguration<InviteFilterExemption>
{
    public int Id { get; init; }
    
    public Guild? Guild { get; init; }

    static void IStaticEntityTypeConfiguration<InviteFilterExemption>.ConfigureBuilder(EntityTypeBuilder<InviteFilterExemption> exemption)
    {
        exemption.ToTable("invite_filter_exemptions");
        exemption.HasKey(x => x.Id);
        exemption.HasIndex(x => x.GuildId);

        exemption.HasPropertyWithColumnName(x => x.Id, "id");
        exemption.HasPropertyWithColumnName(x => x.GuildId, "guild");
        exemption.HasPropertyWithColumnName(x => x.ExemptionType, "type");
        exemption.HasPropertyWithColumnName(x => x.TargetId, "target");
        exemption.HasPropertyWithColumnName(x => x.InviteCode, "invite_code");
    }
}