using Laylua;

namespace Administrator.Bot;

public sealed class LuaMenu(LuaTable buttons, LuaTable msg, LuaFunction callback) : ILuaModel<LuaMenu>
{
    public LuaTable Buttons { get; } = buttons.CloneReference();

    public LuaTable Msg { get; } = msg.CloneReference();

    public LuaFunction Callback { get; } = callback.CloneReference();
}