using Disqord;
using Disqord.Extensions.Interactivity.Menus.Paged;

namespace Administrator.Bot;

public sealed class AdminPagedView(IList<Page> pages, bool isEphemeral = false) : PagedView(new ListPageProvider(pages))
{
    public override void FormatLocalMessage(LocalMessageBase message)
    {
        base.FormatLocalMessage(message);

        if (message is LocalInteractionMessageResponse response)
            response.WithIsEphemeral(isEphemeral);
    }
}