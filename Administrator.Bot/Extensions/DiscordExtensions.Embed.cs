using Administrator.Core;
using Backpack.Net;
using Disqord;

namespace Administrator.Bot;

public static partial class DiscordExtensions
{
    public static LocalEmbed WithQualityColor(this LocalEmbed embed, Quality quality)
    {
        return embed.WithColor(quality switch
        {
            Quality.Normal => Colors.Normal,
            Quality.Genuine => Colors.Genuine,
            Quality.Vintage => Colors.Vintage,
            Quality.Unusual => Colors.Unusual,
            Quality.Unique => Colors.Unique,
            Quality.Community => Colors.Community,
            Quality.Valve => Colors.Valve,
            Quality.SelfMade => Colors.SelfMade,
            Quality.Strange => Colors.Strange,
            Quality.Haunted => Colors.Haunted,
            Quality.Collectors => Colors.Collectors,
            Quality.Decorated => Colors.Decorated,
            _ => throw new ArgumentOutOfRangeException(nameof(quality), quality, null)
        });
    }
    
    public static LocalEmbed WithNormalColor(this LocalEmbed embed)
        => embed.WithColor(Colors.Normal);

    public static LocalEmbed WithUniqueColor(this LocalEmbed embed)
        => embed.WithColor(Colors.Unique);

    public static LocalEmbed WithVintageColor(this LocalEmbed embed)
        => embed.WithColor(Colors.Vintage);

    public static LocalEmbed WithGenuineColor(this LocalEmbed embed)
        => embed.WithColor(Colors.Genuine);

    public static LocalEmbed WithStrangeColor(this LocalEmbed embed)
        => embed.WithColor(Colors.Strange);

    public static LocalEmbed WithUnusualColor(this LocalEmbed embed)
        => embed.WithColor(Colors.Unusual);

    public static LocalEmbed WithHauntedColor(this LocalEmbed embed)
        => embed.WithColor(Colors.Haunted);

    public static LocalEmbed WithCollectorsColor(this LocalEmbed embed)
        => embed.WithColor(Colors.Collectors);

    public static LocalEmbed WithDecoratedColor(this LocalEmbed embed)
        => embed.WithColor(Colors.Decorated);

    public static LocalEmbed WithCommunityColor(this LocalEmbed embed)
        => embed.WithColor(Colors.Community);

    public static LocalEmbed WithSelfMadeColor(this LocalEmbed embed)
        => embed.WithColor(Colors.SelfMade);

    public static LocalEmbed WithValveColor(this LocalEmbed embed)
        => embed.WithColor(Colors.Valve);
}