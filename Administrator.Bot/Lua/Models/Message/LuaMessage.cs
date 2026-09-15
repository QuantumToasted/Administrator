using Laylua.Marshaling;

namespace Administrator.Bot;

[LuaType(Construction = LuaConstructionMode.Factory)]
public partial class LuaMessage
{
    public string? Content { get; set; }
    
    public LuaEmbed[]? Embeds { get; set; }
}