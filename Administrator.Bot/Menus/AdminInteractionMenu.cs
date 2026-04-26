using Disqord;
using Disqord.Extensions.Interactivity.Menus;

namespace Administrator.Bot;

public class AdminInteractionMenu(ViewBase view, IUserInteraction interaction, IDisposable? state = null) : DefaultInteractionMenu(view, interaction)
{
    public override ValueTask DisposeAsync()
    {
        if (View is not null and not AdminPromptView)
        {
            View.ClearComponents();
            return ApplyChangesAsync();
        }
        
        state?.Dispose();

        return base.DisposeAsync();
    }
}