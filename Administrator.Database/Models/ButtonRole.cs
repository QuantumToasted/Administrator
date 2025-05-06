using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record ButtonRole : IButtonRole, IEntityTypeConfiguration<ButtonRole>
{
    public int Id { get; init; }
    
    public Snowflake GuildId { get; init; }
    
    public Snowflake ChannelId { get; init; }
    
    public Snowflake MessageId { get; init; }
    
    public int Row { get; init; }
    
    public int Position { get; init; }

    public string? Emoji { get; set; }

    public string? Text { get; set; }
    
    public LocalButtonComponentStyle Style { get; set; }
    
    public Snowflake RoleId { get; set; }
    
    public int? ExclusiveGroupId { get; set; }
    
    public GuildConfiguration? Guild { get; init; }

    public override string ToString()
        => this.FormatKey();

    public static ButtonRole Create(Snowflake guildId, IMessage message, IRole role, int row, int position, LocalButtonComponentStyle style,
        IEmoji? emoji = null, string? text = null)
    {
        return Create(guildId, message.ChannelId, message.Id, role.Id, row, position, style, emoji, text);
    }
    
    public static ButtonRole Create(Snowflake guildId, Snowflake channelId, Snowflake messageId, Snowflake roleId, int row, int position,
        LocalButtonComponentStyle style, IEmoji? emoji = null, string? text = null)
    {
        return new ButtonRole
        {
            GuildId = guildId,
            ChannelId = channelId,
            MessageId = messageId,
            RoleId = roleId,
            Row = row,
            Position = position,
            Style = style,
            Emoji = emoji?.ToString(),
            Text = text
        };
    }

    IEmoji? IButtonRole.Emoji => LocalCustomEmoji.TryParse(Emoji, out var emoji) ? emoji : Emoji is not null ? new LocalEmoji(Emoji) : null;
    void IEntityTypeConfiguration<ButtonRole>.Configure(EntityTypeBuilder<ButtonRole> buttonRole)
    {
        buttonRole.HasKey(x => x.Id);
        buttonRole.Property(x => x.Emoji).HasMaxLength(100);
        buttonRole.Property(x => x.Text).HasMaxLength(Discord.Limits.Component.Button.MaxLabelLength);
    }
}