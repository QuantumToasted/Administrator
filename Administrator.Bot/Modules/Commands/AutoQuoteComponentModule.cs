using Disqord.Bot.Commands.Components;
using Qmmands;

namespace Administrator.Bot;

public sealed partial class AutoQuoteComponentModule
{
    [ButtonCommand("AutoQuote:Delete")]
    public partial Task Delete();
}