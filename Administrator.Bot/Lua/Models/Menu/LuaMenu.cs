using Laylua;
using Laylua.Marshaling;

namespace Administrator.Bot;

[LuaType] // TODO: reduce LuaTable dependencies
public sealed partial class LuaMenu
{
    public LuaTable? Buttons { get; set; }

    public LuaMessage? Msg { get; set; }

    public LuaFunction? Callback { get; set; }

    public static LuaMenu Create(LuaTable buttons, LuaMessage msg, LuaFunction callback)
    {
        return new LuaMenu
        {
            Buttons = buttons.CloneReference(),
            Msg = msg,
            Callback = callback.CloneReference()
        };
    }
}