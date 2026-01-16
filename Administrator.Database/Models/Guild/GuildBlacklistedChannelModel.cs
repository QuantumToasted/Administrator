using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed class GuildBlacklistedChannelModel : IGuildBlacklistedChannel, IEntityTypeConfiguration<GuildBlacklistedChannelModel>
{
    public ulong GuildId { get; init; }
    
    public ulong ChannelId { get; init; }

    Snowflake IGuildBlacklistedChannel.GuildId => GuildId;
    Snowflake IGuildBlacklistedChannel.ChannelId => ChannelId;
    void IEntityTypeConfiguration<GuildBlacklistedChannelModel>.Configure(EntityTypeBuilder<GuildBlacklistedChannelModel> builder)
    {
        throw new NotImplementedException();
    }
}