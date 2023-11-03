using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record LuaCommand(Snowflake GuildId, string Name, byte[] Metadata, byte[] Command) : IStaticEntityTypeConfiguration<LuaCommand>
{
    public byte[] Metadata { get; set; } = Metadata;

    public byte[] Command { get; set; } = Command;
    
    public Guild? Guild { get; init; }

    static void IStaticEntityTypeConfiguration<LuaCommand>.ConfigureBuilder(EntityTypeBuilder<LuaCommand> command)
    {
        command.ToTable("lua_commands");
        command.HasKey(x => new { x.GuildId, x.Name });

        command.HasPropertyWithColumnName(x => x.GuildId, "guild");
        command.HasPropertyWithColumnName(x => x.Name, "name");
        command.HasPropertyWithColumnName(x => x.Metadata, "metadata");
        command.HasPropertyWithColumnName(x => x.Command, "command");
    }
}