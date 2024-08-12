using Disqord.Extensions.Interactivity.Menus;

namespace Administrator.Bot;

public sealed class AdminTextMenu(ViewBase view) : DefaultTextMenu(view)
{
    public bool ClearComponents { get; init; } = true;
    
    public override ValueTask DisposeAsync()
    {
        if (View is not null && ClearComponents)
        {
            View.ClearComponents();
            return ApplyChangesAsync();
        }
        
        return base.DisposeAsync();
    }
}