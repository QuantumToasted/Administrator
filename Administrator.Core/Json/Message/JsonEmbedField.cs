using System.Text.Json.Serialization;
using Disqord;
using Qmmands;
using Qommon;

namespace Administrator.Core;

public sealed class JsonEmbedField
{
    public string Name { get; init; } = null!;

    public string Value { get; init; } = null!;

    [JsonPropertyName("Inline")]
    public bool? IsInline { get; init; }
    
    public static JsonEmbedField FromEmbedField(IEmbedField field)
    {
        return new JsonEmbedField
        {
            Name = field.Name,
            Value = field.Value,
            IsInline = field.IsInline
        };
    }

    public static JsonEmbedField FromEmbedField(LocalEmbedField field)
    {
        return new JsonEmbedField
        {
            Name = field.Name.Value,
            Value = field.Value.Value,
            IsInline = field.IsInline.GetValueOrDefault()
        };
    }
}