using Disqord;
using Laylua.Marshaling;

namespace Administrator.Bot;

[LuaType(Construction = LuaConstructionMode.Factory)]
public sealed partial class LuaEmbedField : IEmbedField
{
    public required string Name { get; set; }
    
    public required string Value { get; set; }
    
    [LuaName("inline")]
    public bool IsInline { get; set; }
}