using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

// TODO: implement button roles
public sealed record ButtonRole(Snowflake GuildId, Snowflake ChannelId, Snowflake MessageId, int Row, int Position, string? Emoji, string? Text, Snowflake RoleId) 
    : IStaticEntityTypeConfiguration<ButtonRole>
{
    public int Id { get; init; }
    
    public int? ExclusiveGroupId { get; set; }
    
    public Guild? Guild { get; init; }

    static void IStaticEntityTypeConfiguration<ButtonRole>.ConfigureBuilder(EntityTypeBuilder<ButtonRole> buttonRole)
    {
        buttonRole.ToTable("button_roles");
        buttonRole.HasKey(x => x.Id);

        buttonRole.HasPropertyWithColumnName(x => x.Id, "id");
        buttonRole.HasPropertyWithColumnName(x => x.GuildId, "guild");
        buttonRole.HasPropertyWithColumnName(x => x.ChannelId, "channel");
        buttonRole.HasPropertyWithColumnName(x => x.MessageId, "message");
        buttonRole.HasPropertyWithColumnName(x => x.Row, "row");
        buttonRole.HasPropertyWithColumnName(x => x.Position, "position");
        buttonRole.HasPropertyWithColumnName(x => x.Emoji, "emoji");
        buttonRole.HasPropertyWithColumnName(x => x.Text, "text");
        buttonRole.HasPropertyWithColumnName(x => x.RoleId, "role");
        buttonRole.HasPropertyWithColumnName(x => x.ExclusiveGroupId, "exclusive_group");
    }
}