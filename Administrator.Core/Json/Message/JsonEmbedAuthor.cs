using System.Text.Json.Serialization;
using Disqord;
using Qmmands;
using Qommon;

namespace Administrator.Core;

public sealed class JsonEmbedAuthor
{
    public string Name { get; init; } = null!;

    [JsonPropertyName("icon")]
    public string? IconUrl { get; init; }

    public string? Url { get; init; }

    public static JsonEmbedAuthor FromEmbedAuthor(IEmbedAuthor author)
    {
        return new JsonEmbedAuthor
        {
            Name = author.Name,
            IconUrl = author.IconUrl,
            Url = author.Url
        };
    }

    public static JsonEmbedAuthor FromEmbedAuthor(LocalEmbedAuthor author)
    {
        return new JsonEmbedAuthor
        {
            Name = author.Name.Value,
            IconUrl = author.IconUrl.GetValueOrDefault(),
            Url = author.Url.GetValueOrDefault()
        };
    }
}