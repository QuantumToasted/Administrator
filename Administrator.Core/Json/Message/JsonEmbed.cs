using Disqord;
using System.Text.Json.Serialization;
using NodaTime;
using NodaTime.Extensions;
using Qommon;

namespace Administrator.Core;

public sealed class JsonEmbed
{
    [JsonConverter(typeof(ColorJsonConverter))]
    public Color? Color { get; init; }

    public JsonEmbedAuthor? Author { get; init; }

    public string? Url { get; init; }

    public string? Title { get; init; }

    public string? Description { get; init; }

    public List<JsonEmbedField>? Fields { get; init; }

    [JsonPropertyName("Image")]
    public string? ImageUrl { get; init; }

    [JsonPropertyName("Thumbnail")]
    public string? ThumbnailUrl { get; init; }

    public JsonEmbedFooter? Footer { get; init; }

    [JsonConverter(typeof(InstantJsonConverter))]
    public Instant? Timestamp { get; init; }

    public static JsonEmbed FromEmbed(IEmbed embed)
        => FromEmbed(LocalEmbed.CreateFrom(embed));

    public static JsonEmbed FromEmbed(LocalEmbed embed)
    {
        return new JsonEmbed
        {
            Color = embed.Color.GetValueOrNullable(),
            Author = embed.Author.GetValueOrDefault() is { } author ? JsonEmbedAuthor.FromEmbedAuthor(author) : null,
            Url = embed.Url.GetValueOrDefault(),
            Title = embed.Title.GetValueOrDefault(),
            Description = embed.Description.GetValueOrDefault(),
            Fields = embed.Fields.GetValueOrDefault()?.Select(JsonEmbedField.FromEmbedField).ToList(),
            ImageUrl = embed.ImageUrl.GetValueOrDefault(),
            ThumbnailUrl = embed.ThumbnailUrl.GetValueOrDefault(),
            Footer = embed.Footer.GetValueOrDefault() is { } footer ? JsonEmbedFooter.FromEmbedFooter(footer) : null,
            Timestamp = embed.Timestamp.GetValueOrNullable()?.ToInstant()
        };
    }
}