using Disqord;

namespace Administrator.Bot;

public sealed class LuaEmbed(IEmbed embed) : ILuaModel<LuaEmbed>
{
    public string? Title { get; } = embed.Title;

    public string? Description { get; } = embed.Description;

    public string? Timestamp { get; } = embed.Timestamp?.ToString("s");

    public string? Color { get; } = embed.Color?.ToString();

    public LuaEmbedFooter? Footer { get; } = embed.Footer is { } footer ? new LuaEmbedFooter(footer) : null;

    public LuaEmbedAuthor? Author { get; } = embed.Author is { } author ? new LuaEmbedAuthor(author) : null;

    public LuaEmbedField[] Fields { get; } = embed.Fields.Select(x => new LuaEmbedField(x)).ToArray();
}