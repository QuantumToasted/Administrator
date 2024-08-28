using Disqord;
using Disqord.Bot.Commands.Application;

namespace Administrator.Bot;

public sealed partial class TagImportModule
{
    [MessageCommand("Convert to Tag")]
    public partial Task ConvertToTag(IMessage message);
}