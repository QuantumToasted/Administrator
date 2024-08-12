using Disqord;
using Humanizer;

namespace Administrator.Bot;

public class LuaChannel(IChannel channel)
{
    public long Id { get; } = (long) channel.Id.RawValue;
    
    public string Name { get; } = channel.Name;

    public string Type { get; } = channel.Type.ToString().ToLower();
}