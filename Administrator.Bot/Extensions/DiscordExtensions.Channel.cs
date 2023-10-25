using Disqord;
using Disqord.Gateway;
using Disqord.Rest;

namespace Administrator.Bot;

public static partial class DiscordExtensions
{
    public static async ValueTask<IMessage?> GetOrFetchMessageAsync(this IMessageChannel channel, Snowflake messageId)
    {
        var client = (IGatewayClient)channel.Client;

        return client.GetMessage(channel.Id, messageId) ?? await channel.FetchMessageAsync(messageId);
    }
}