using Disqord;
using Disqord.Rest;
using Laylua;

namespace Administrator.Bot;

public sealed partial class DiscordRestLuaLibrary
{
    private partial IEnumerable<string> RegisterMessageRestMethods(Lua lua)
    {
        yield return lua.SetStringGlobal("sendMessage", (Func<long, LuaTable, long>) SendMessage);
        yield return lua.SetStringGlobal("modifyMessage", (Action<long, long, LuaTable>)ModifyMessage);
    }
    
    private long SendMessage(long channelId, LuaTable msg)
    {
        var message = ConvertMessage<LocalMessage>(msg);
        return RunWait(async () =>
        {
            var newMessage = await bot.SendMessageAsync((ulong)channelId, message);
            return (long)(ulong)newMessage.Id;
        });
    }

    private void ModifyMessage(long channelId, long messageId, LuaTable msg)
    {
        var message = ConvertMessage<LocalMessage>(msg);
        RunWait(() => bot.TryModifyMessageToAsync((ulong)channelId, (ulong)messageId, message));
    }
}