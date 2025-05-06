using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record InviteFilterExemption : IInviteFilterExemption, IEntityTypeConfiguration<InviteFilterExemption>
{
    public int Id { get; init; }
    
    public Snowflake GuildId { get; init; }
    
    public InviteFilterExemptionType ExemptionType { get; init; }
    
    public Snowflake? TargetId { get; init; }
    
    public string? InviteCode { get; init; }
    
    public GuildConfiguration? Guild { get; init; }
    
    public override string ToString()
        => this.FormatKey();

    public static InviteFilterExemption FromGuild(Snowflake guildId, Snowflake targetGuildId)
        => Create(guildId, InviteFilterExemptionType.Guild, targetGuildId);

    public static InviteFilterExemption FromChannel(IGuildChannel channel) => FromChannel(channel.GuildId, channel.Id);
    public static InviteFilterExemption FromChannel(Snowflake guildId, Snowflake targetChannelId)
        => Create(guildId, InviteFilterExemptionType.Channel, targetChannelId);

    public static InviteFilterExemption FromRole(IRole role) => FromRole(role.GuildId, role.Id);
    public static InviteFilterExemption FromRole(Snowflake guildId, Snowflake targetRoleId)
        => Create(guildId, InviteFilterExemptionType.Role, targetRoleId);

    public static InviteFilterExemption FromUser(IMember member) => FromUser(member.GuildId, member.Id);
    public static InviteFilterExemption FromUser(Snowflake guildId, Snowflake targetUserId)
        => Create(guildId, InviteFilterExemptionType.User, targetUserId);
    
    public static InviteFilterExemption FromInviteCode(Snowflake guildId, string inviteCode)
        => Create(guildId, InviteFilterExemptionType.InviteCode, inviteCode: inviteCode);

    public static InviteFilterExemption Create(Snowflake guildId, InviteFilterExemptionType exemptionType, Snowflake? targetId = null, string? inviteCode = null)
    {
        return new InviteFilterExemption
        {
            GuildId = guildId,
            ExemptionType = exemptionType,
            TargetId = targetId,
            InviteCode = inviteCode
        };
    }
    
    void IEntityTypeConfiguration<InviteFilterExemption>.Configure(EntityTypeBuilder<InviteFilterExemption> exemption)
    {
        exemption.HasKey(x => x.Id);
        exemption.HasIndex(x => x.GuildId);
        exemption.Property(x => x.InviteCode).HasMaxLength(50); // no clue how long invites will ever get...
    }
}