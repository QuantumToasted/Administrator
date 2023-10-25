using Disqord;
using Disqord.Extensions.Interactivity.Menus;

namespace Administrator.Bot;

public class AdminInteractionMenu(ViewBase view, IUserInteraction interaction) : DefaultInteractionMenu(view, interaction)
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