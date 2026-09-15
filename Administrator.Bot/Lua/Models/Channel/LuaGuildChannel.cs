using Disqord;
using Disqord.Bot;
using Disqord.Rest.Api;
using Laylua.Marshaling;
using Qommon;

namespace Administrator.Bot;

[LuaType]
public partial class LuaGuildChannel(IGuildChannel channel)
{
    public Snowflake Id { get; } = channel.Id;
    
    public string Name { get; } = channel.Name;

    public string Type { get; } = channel.Type.ToString().ToLower();
    
    public string Mention { get; } = Disqord.Mention.Channel(channel.Id);
    
    public int Position { get; } = channel.Position;
    
    public async Task<bool> SetName(string name)
    {
        try
        {
            Guard.IsNotNullOrWhiteSpace(name);

            var bot = (DiscordBotBase)channel.Client;
            await bot.ApiClient.ModifyChannelAsync(Id, new ModifyChannelJsonRestRequestContent { Name = name });
            return true;
        }
        catch
        {
            return false;
        }
    }
}