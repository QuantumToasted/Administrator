using Disqord;
using Disqord.Rest;
using Disqord.Rest.Api;
using Laylua;
using Qommon;

namespace Administrator.Bot;

public sealed partial class DiscordRestLuaLibrary
{
    private partial IEnumerable<string> RegisterChannelRestMethods(Lua lua)
    {
        yield return lua.SetStringGlobal("createTextChannel", (Func<string, long?, LuaTable>)CreateTextChannel);
        yield return lua.SetStringGlobal("createVoiceChannel", (Func<string, long?, LuaTable>)CreateVoiceChannel);
        yield return lua.SetStringGlobal("createCategoryChannel", (Func<string, LuaTable>)CreateCategoryChannel);
        yield return lua.SetStringGlobal("modifyChannelName", (Action<long, string>)ModifyChannelName);
        yield return lua.SetStringGlobal("modifyChannelPosition", (Action<long, int>)ModifyChannelPosition);
        yield return lua.SetStringGlobal("modifyChannelTopic", (Action<long, string>)ModifyChannelTopic);
        yield return lua.SetStringGlobal("deleteChannel", (Action<long>)DeleteChannel);
    }
    
    private LuaTable CreateTextChannel(string name, long? categoryId = null)
    {
        return RunWait(async () =>
        {
            var channel = await bot.CreateTextChannelAsync(guildId, name, x =>
            {
                if (categoryId.HasValue)
                    x.CategoryId = (Snowflake)(ulong)categoryId.Value;
            });

            return _lua.ConvertEntity(channel);
        });
    }
    
    private LuaTable CreateVoiceChannel(string name, long? categoryId = null)
    {
        return RunWait(async () =>
        {
            var channel = await bot.CreateVoiceChannelAsync(guildId, name, x =>
            {
                if (categoryId.HasValue)
                    x.CategoryId = (Snowflake)(ulong)categoryId.Value;
            });

            return _lua.ConvertEntity(channel);
        });
    }

    private LuaTable CreateCategoryChannel(string name)
    {
        return RunWait(async () =>
        {
            var channel = await bot.CreateCategoryChannelAsync(guildId, name);
            return _lua.ConvertEntity(channel);
        });
    }

    private void ModifyChannelName(long channelId, string name)
    {
        Guard.IsNotNullOrWhiteSpace(name);
        RunWait(() => bot.ApiClient.ModifyChannelAsync((ulong)channelId, new ModifyChannelJsonRestRequestContent { Name = name }));
    }

    private void ModifyChannelPosition(long channelId, int position)
    {
        RunWait(() => bot.ApiClient.ModifyChannelAsync((ulong)channelId, new ModifyChannelJsonRestRequestContent { Position = position }));
    }

    private void ModifyChannelTopic(long channelId, string? topic)
    {
        topic ??= string.Empty;
        RunWait(() => bot.ApiClient.ModifyChannelAsync((ulong)channelId, new ModifyChannelJsonRestRequestContent { Topic = topic }));
    }

    private void DeleteChannel(long channelId)
    {
        RunWait(() => bot.DeleteChannelAsync((ulong)channelId));
    }
}