using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record Block : RevocablePunishment, IBlock, IEntityTypeConfiguration<Block>
{
    public Snowflake ChannelId { get; init; }
    
    public DateTimeOffset? ExpiresAt { get; init; }
    // Permissions? PreviousChannelAllowPermissions, Permissions? PreviousChannelDenyPermissions
    
    public Permissions? PreviousChannelAllowPermissions { get; init; }
    
    public Permissions? PreviousChannelDenyPermissions { get; init; }
    
    public override PunishmentType Type => PunishmentType.Block;

    void IEntityTypeConfiguration<Block>.Configure(EntityTypeBuilder<Block> block)
    {
        block.HasBaseType<RevocablePunishment>();
    }
}