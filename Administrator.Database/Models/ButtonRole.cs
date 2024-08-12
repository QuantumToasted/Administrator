using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record ButtonRole(Snowflake GuildId, Snowflake ChannelId, Snowflake MessageId, int Row, int Position, string? Emoji, string? Text, LocalButtonComponentStyle Style, Snowflake RoleId)
    : INumberKeyedDbEntity<int>
{
    public int Id { get; init; }
    
    public int? ExclusiveGroupId { get; set; }
    
    public Guild? Guild { get; init; }

    public override string ToString()
        => this.FormatKey();

    private sealed class ButtonRoleConfiguration : IEntityTypeConfiguration<ButtonRole>
    {
        public void Configure(EntityTypeBuilder<ButtonRole> buttonRole)
        {
            buttonRole.HasKey(x => x.Id);
        }
    }
}