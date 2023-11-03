using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record Block(Snowflake GuildId, UserSnapshot Target, UserSnapshot Moderator, string? Reason,
        Snowflake ChannelId, DateTimeOffset? ExpiresAt, Permissions? PreviousChannelAllowPermissions, Permissions? PreviousChannelDenyPermissions)
    : RevocablePunishment(GuildId, Target, Moderator, Reason), IExpiringDbEntity, IStaticEntityTypeConfiguration<Block>
{
    //public override PunishmentType Type { get; init; } = PunishmentType.Block;
    
    static void IStaticEntityTypeConfiguration<Block>.ConfigureBuilder(EntityTypeBuilder<Block> block)
    {
        block.HasBaseType<RevocablePunishment>();

        block.HasPropertyWithColumnName(x => x.ChannelId, "channel");
        block.HasPropertyWithColumnName(x => x.ExpiresAt, "expires");
        block.HasPropertyWithColumnName(x => x.PreviousChannelAllowPermissions, "previous_allow_permissions");
        block.HasPropertyWithColumnName(x => x.PreviousChannelDenyPermissions, "previous_deny_permissions");
    }
}