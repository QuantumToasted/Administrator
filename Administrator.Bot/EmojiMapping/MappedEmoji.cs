using System.Text.Json.Serialization;
using Disqord;

namespace Administrator.Bot;

public sealed record MappedEmoji(
    [property: JsonPropertyName("primaryName")]
        string PrimaryName,
    [property: JsonPropertyName("primaryNameWithColons")]
        string PrimaryNameWithColons,
    [property: JsonPropertyName("names")] 
        string[] Names,
    [property: JsonPropertyName("namesWithColons")]
        string[] NamesWithColons,
    [property: JsonPropertyName("surrogates")]
        string Surrogates,
    [property: JsonPropertyName("surrogatesAlternate")]
        string? AlternateSurrogates,
    [property: JsonPropertyName("utf32codepoints")]
        long[] Utf32Codepoints,
    [property: JsonPropertyName("assetFileName")]
        string AssetFileName,
    [property: JsonPropertyName("assetUrl")]
        Uri AssetUrl) : IEmoji
{
    public override string ToString()
        => Surrogates;

    public bool Equals(MappedEmoji? other)
        => other?.Surrogates == Surrogates;

    public override int GetHashCode()
        => Comparers.Emoji.GetHashCode(this);

    public static implicit operator LocalEmoji(MappedEmoji emoji)
        => LocalEmoji.Unicode(emoji.Surrogates);

    string? IPossiblyNamableEntity.Name => Surrogates;
    bool IEquatable<IEmoji>.Equals(IEmoji? other) => other switch
    {
        LocalEmoji localEmoji => localEmoji.Name.Equals(Surrogates),
        MappedEmoji mappedEmoji => mappedEmoji.Surrogates.Equals(Surrogates),
        _ => false
    };
}