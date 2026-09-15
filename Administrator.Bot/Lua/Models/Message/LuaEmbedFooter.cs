using Disqord;
using Laylua.Marshaling;

namespace Administrator.Bot;

[LuaType(Construction = LuaConstructionMode.Factory)]
public sealed partial class LuaEmbedFooter : IEmbedFooter
{
    public string? Text { get; set; }
    
    public string? IconUrl { get; set; }
    
    string? IEmbedFooter.ProxyIconUrl => null;
}