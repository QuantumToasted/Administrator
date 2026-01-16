using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed class GuildConfiguration : IGuildConfiguration, IEntityTypeConfiguration<GuildConfiguration>
{
    public ulong GuildId { get; }
    
    public void Configure(EntityTypeBuilder<GuildConfiguration> guildConfiguration)
    {
        throw new NotImplementedException();
    }

    Snowflake IGuildConfiguration.GuildId => GuildId;
}