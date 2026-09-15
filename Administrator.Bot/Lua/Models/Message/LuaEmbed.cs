using Disqord;
using Laylua.Marshaling;

namespace Administrator.Bot;

[LuaType(Construction = LuaConstructionMode.Factory)]
public sealed partial class LuaEmbed : IEmbed
{
    public string? Title { get; set; }
    
    public string? Description { get; set; }
    
    public string? Url { get; set; }
    
    public long? Timestamp { get; set; }
    
    public string? Color { get; set; }
    
    [LuaName("image")]
    public string? ImageUrl { get; set; }
    
    [LuaName("thumbnail")]
    public string? ThumbnailUrl { get; set; }
    
    public LuaEmbedFooter? Footer { get; set; }
    
    public LuaEmbedAuthor? Author { get; set; }
    
    public LuaEmbedField[]? Fields { get; set; }

    string IEmbed.Type => "rich";
    DateTimeOffset? IEmbed.Timestamp => Timestamp is { } seconds ? DateTimeOffset.FromUnixTimeSeconds(seconds) : null;
    Color? IEmbed.Color => Disqord.Color.TryParse(Color, out var color) ? color : null;
    IEmbedImage? IEmbed.Image => TransientEmbedImage.FromImageUrl(ImageUrl);
    IEmbedThumbnail? IEmbed.Thumbnail => TransientEmbedThumbnail.FromThumbnailUrl(ThumbnailUrl);
    IEmbedVideo? IEmbed.Video => null;
    IEmbedProvider? IEmbed.Provider => null;
    IEmbedFooter? IEmbed.Footer => Footer;
    IEmbedAuthor? IEmbed.Author => Author;
    IReadOnlyList<IEmbedField> IEmbed.Fields => Fields?.ToList() ?? [];
}