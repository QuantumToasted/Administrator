using Disqord;
using Disqord.Extensions.Interactivity.Menus;
using Qommon;

namespace Administrator.Bot;

public sealed class LuaButtonEvent(ButtonEventArgs e, LuaMenuView view) : ILuaModel<LuaButtonEvent>
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