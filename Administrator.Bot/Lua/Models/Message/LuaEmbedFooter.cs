using Disqord;

namespace Administrator.Bot;

public sealed class LuaEmbedFooter(IEmbedFooter footer) : ILuaModel<LuaEmbedFooter>
{
    public string Text { get; } = footer.Text;

    public string? IconUrl { get; } = footer.IconUrl;
}