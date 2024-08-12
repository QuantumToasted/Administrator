using System.Reflection;
using Disqord.Bot.Commands.Application;
using Laylua;
using Laylua.Marshaling;
using Microsoft.Extensions.DependencyInjection;

namespace Administrator.Bot;

public static class LuaExtensions
{
    public static TValue? GetValueOrDefault<TKey, TValue>(this LuaTable table, TKey key)
        where TKey : notnull
        where TValue : class
    {
        return table.TryGetValue<TKey, TValue>(key, out var value) ? value : null;
    }
    
    public static string SetStringGlobal<T>(this Lua lua, string key, T value)
    {
        lua.SetGlobal(key, value);
        return key;
    }
    
    public static void OpenDiscordLibraries(this Lua lua, IDiscordApplicationGuildCommandContext context, CancellationToken cancellationToken, bool setHook = true)
    {
        lua.OpenLibrary(new DiscordCommandContextLibrary(context, cancellationToken));
        lua.OpenLibrary(new DiscordEnumLibrary(context.Bot));
        lua.OpenLibrary(new DiscordHttpLibrary(context.Services.GetRequiredService<HttpClient>(), cancellationToken));
        lua.OpenLibrary(new DiscordJsonLibrary(cancellationToken));
        lua.OpenLibrary(new DiscordPersistenceLibrary(context, cancellationToken));
        
        if (setHook)
            lua.State.Hook = new LuaMultiHook(cancellationToken);
    }

    public static void SetModelDescriptors(this DefaultUserDataDescriptorProvider provider)
    {
        foreach (var type in typeof(ILuaModel).Assembly.GetTypes().Where(x => typeof(ILuaModel).IsAssignableFrom(x) && !x.IsInterface))
        {
            var method = typeof(ILuaModel<>).MakeGenericType(type).GetMethod(nameof(ILuaModel.SetUserDataDescriptor), BindingFlags.Static | BindingFlags.Public)!;
            method.Invoke(null, [provider]);
        }
    }
}