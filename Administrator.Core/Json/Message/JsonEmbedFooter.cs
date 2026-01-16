using System.Text.Json.Serialization;
using Disqord;
using Qmmands;
using Qommon;

namespace Administrator.Core;

public sealed class JsonEmbedFooter
{
    public string Text { get; init; } = null!;

    [JsonPropertyName("icon")]
    public string? IconUrl { get; init; }

    public static JsonEmbedFooter FromEmbedFooter(IEmbedFooter footer)
    {
        return new JsonEmbedFooter
        {
            Text = footer.Text,
            IconUrl = footer.IconUrl
        };
    }

    public static JsonEmbedFooter FromEmbedFooter(LocalEmbedFooter footer)
    {
        return new JsonEmbedFooter
        {
            Text = footer.Text.Value,
            IconUrl = footer.IconUrl.GetValueOrDefault()
        };
    }
}