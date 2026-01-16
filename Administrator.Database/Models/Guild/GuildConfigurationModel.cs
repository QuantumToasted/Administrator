using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed class GuildConfigurationModel : IGuildConfiguration, IEntityTypeConfiguration<GuildConfigurationModel>
{
    public ulong GuildId { get; init; }

    Snowflake IGuildConfiguration.GuildId => GuildId;
    void IEntityTypeConfiguration<GuildConfigurationModel>.Configure(EntityTypeBuilder<GuildConfigurationModel> guildConfiguration)
    {
        throw new NotImplementedException();
    }
}