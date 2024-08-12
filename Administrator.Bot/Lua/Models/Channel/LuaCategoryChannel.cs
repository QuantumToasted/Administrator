using Disqord;
using Disqord.Rest;

namespace Administrator.Bot;

public sealed class LuaCategoryChannel(ICategoryChannel channel, DiscordLuaLibraryBase library) : LuaGuildChannel(channel), ILuaModel<LuaCategoryChannel>
{
    public void SetName(string name)
        => library.RunWait(ct => channel.ModifyAsync(x => x.Name = name, cancellationToken: ct));
}