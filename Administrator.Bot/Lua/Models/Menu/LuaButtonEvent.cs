using Disqord;
using Disqord.Extensions.Interactivity.Menus;
using Laylua.Marshaling;
using Qommon;

namespace Administrator.Bot;

[LuaType] // TODO: reduce LuaTable dependencies
public sealed partial class LuaButtonEvent(ButtonEventArgs e, LuaMenuView view)
{
    public string CustomId { get; } = e.Button.CustomId;

    public string Name { get; private set; } = e.Button.Label!;

    public long Style { get; private set; } = (long)e.Button.Style;

    public void SetName(string name)
    {
        Guard.IsNotNullOrWhiteSpace(name);
        
        e.Button.Label = name;
        Name = name;
        view.ReportChanges();
    }

    public void SetStyle(long style)
    {
        var styleEnum = (LocalButtonComponentStyle)style;
        e.Button.Style = styleEnum;
        view.ReportChanges();
    }

    public void SetDisabled(bool disabled)
    {
        e.Button.IsDisabled = disabled;
        view.ReportChanges();
    }
}