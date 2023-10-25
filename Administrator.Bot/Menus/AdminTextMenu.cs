using Disqord;
using Disqord.Extensions.Interactivity.Menus;

namespace Administrator.Bot;

public class AdminTextMenu(ViewBase view, Snowflake messageId) : DefaultTextMenu(view, messageId)
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