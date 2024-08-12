using Disqord;

namespace Administrator.Bot;

public sealed class LuaEmbedField(IEmbedField field) : ILuaModel<LuaEmbed>
{
    public string Name { get; } = field.Name;

    public string Value { get; } = field.Value;

    public bool Inline { get; } = field.IsInline;
}