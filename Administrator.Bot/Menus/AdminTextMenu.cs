using Disqord.Extensions.Interactivity.Menus;

namespace Administrator.Bot;

public class AdminTextMenu(ViewBase view) : DefaultTextMenu(view)
{
    public override ValueTask DisposeAsync()
    {
        if (View is not null)
        {
            View.ClearComponents();
            return ApplyChangesAsync();
        }
        
        return base.DisposeAsync();
    }
}