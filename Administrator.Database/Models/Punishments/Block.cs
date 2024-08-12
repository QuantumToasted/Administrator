using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record Block(Snowflake GuildId, UserSnapshot Target, UserSnapshot Moderator, string? Reason, Snowflake ChannelId, DateTimeOffset? ExpiresAt, Permissions? PreviousChannelAllowPermissions, Permissions? PreviousChannelDenyPermissions)
    : RevocablePunishment(GuildId, Target, Moderator, Reason), IExpiringDbEntity, IBlock
{
    public override PunishmentType Type => PunishmentType.Block;

    private sealed class BlockConfiguration : IEntityTypeConfiguration<Block>
    {
        public void Configure(EntityTypeBuilder<Block> block)
        {
            block.HasBaseType<RevocablePunishment>();
        }
    }
}