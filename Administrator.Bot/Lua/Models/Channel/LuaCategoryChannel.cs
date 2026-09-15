using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using Disqord.Rest;
using Laylua.Marshaling;

namespace Administrator.Bot;

[LuaType]
public sealed partial class LuaCategoryChannel(ICategoryChannel channel) : LuaGuildChannel(channel)
{
    [LuaName("channels")]
    public Snowflake[] ChannelIds { get; } = GetCategoryChannels(channel);

    private static Snowflake[] GetCategoryChannels(ICategoryChannel channel)
    {
        var bot = (DiscordBotBase)channel.Client;
        return bot.GetChannels(channel.GuildId).Values.Where(x => (x as ICategorizableGuildChannel)?.CategoryId == channel.Id).Select(x => x.Id).ToArray();
    }
}