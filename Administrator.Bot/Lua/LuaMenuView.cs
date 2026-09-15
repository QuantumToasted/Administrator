using Disqord;
using Disqord.Extensions.Interactivity.Menus;
using Laylua;
using Laylua.Moon;
using Qommon;

namespace Administrator.Bot;

public sealed class LuaMenuView : AdminViewBase
{
    private readonly LuaFunction _callback;

    public LuaMenuView(LuaMenu menu) : this(menu.Buttons, menu.Msg, menu.Callback) { }
    public LuaMenuView(LuaTable buttons, LuaMessage msg, LuaFunction callback) : base(null)
    {
        _callback = callback.CloneReference();

        foreach (var (k, v) in buttons)
        {
            var button = new ButtonViewComponent(HandleButton);
            
            switch (k.Type)
            {
                // {"foo", "bar"}
                case LuaType.Number when v.Value is string name:
                    button.Label = name;
                    break;
                // {["foo"]=style,["bar"]=style}
                case LuaType.String when v.Value is long style:
                    button.Label = k.Value!.ToString();
                    button.Style = (LocalButtonComponentStyle)style;
                    break;
            }

            AddComponent(button);
        }

        Message = LocalMessage.CreateFrom(msg);
    }

    public LocalMessage Message { get; private set; }

    public new void ReportChanges() => base.ReportChanges();

    private ValueTask HandleButton(ButtonEventArgs e)
    {
        var button = new LuaButtonEvent(e, this);
        using var res = _callback.Call(button);
        if (res is { IsEmpty: false } && res.First.TryGetValue(out LuaMessage? message))
        {
            try
            {
                Message = LocalMessage.CreateFrom(message!);
                ReportChanges();
            } 
            catch { /* ignored */ }
        }

        return ValueTask.CompletedTask;
    }

    public override void FormatLocalMessage(LocalMessageBase message)
    {
        base.FormatLocalMessage(message);
            
        message.Content = Message.Content;
        message.Embeds = Message.Embeds;
    }
}