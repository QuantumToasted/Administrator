using Disqord;
using Qmmands;
using Qommon;

namespace Administrator.Core;

public class JsonEmbedField
{
    public string Name { get; init; } = null!;

    public string Value { get; init; } = null!;

    public bool? IsInline { get; init; }

    public async ValueTask<LocalEmbedField> ToLocalFieldAsync(IPlaceholderFormatter formatter, ICommandContext? context = null)
    {
        return new LocalEmbedField()
            .WithName(await formatter.ReplacePlaceholdersAsync(Name, context))
            .WithValue(await formatter.ReplacePlaceholdersAsync(Value, context))
            .WithIsInline(IsInline ?? true);
    }

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