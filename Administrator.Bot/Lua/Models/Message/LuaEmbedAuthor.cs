using Disqord;

namespace Administrator.Bot;

public sealed class LuaEmbedAuthor(IEmbedAuthor author) : ILuaModel<LuaEmbedAuthor>
{
    public string Name { get; } = author.Name;

    public string? Url { get; } = author.Url;

    public string? IconUrl { get; } = author.IconUrl;
}