using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Gateway;

namespace Administrator.Bot;

public sealed class MessageEditViewService : DiscordBotService
{
    public Dictionary<Snowflake, MessageEditView> Views { get; } = new();

    protected override ValueTask OnMessageDeleted(MessageDeletedEventArgs e)
    {
        Views.Remove(e.MessageId);
        return ValueTask.CompletedTask;
    }
}