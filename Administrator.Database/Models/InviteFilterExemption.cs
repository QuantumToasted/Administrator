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
{
    public int Id { get; init; }
    
    public Guild? Guild { get; init; }

    private sealed class InviteFilterExemptionConfiguration : IEntityTypeConfiguration<InviteFilterExemption>
    {
        public void Configure(EntityTypeBuilder<InviteFilterExemption> exemption)
        {
            exemption.ToTable("invite_filter_exemptions");
            exemption.HasKey(x => x.Id);
            exemption.HasIndex(x => x.GuildId);
        }
    }
}