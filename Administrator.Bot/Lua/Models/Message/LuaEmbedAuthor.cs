using Disqord;
using Laylua.Marshaling;

namespace Administrator.Bot;

[LuaType(Construction = LuaConstructionMode.Factory)]
public sealed partial class LuaEmbedAuthor : IEmbedAuthor
{
    public required string Name { get; set; }
    
    public string? Url { get; set; }
    
    [LuaName("icon")]
    public string? IconUrl { get; set; }
    
    string? IEmbedAuthor.ProxyIconUrl => null;
}