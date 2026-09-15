using System.Text;
using Administrator.Core;
using Laylua;
using Microsoft.EntityFrameworkCore;
using Qommon.Metadata;

namespace Administrator.Bot;

public sealed class DiscordPersistenceLibrary(AdminLuaContext context) 
    : DiscordLuaLibrary<DiscordPersistenceLibrary>(context)
{
    private const int MAX_PERSISTENCE_LENGTH = 1_000_000;
    
    public override string Name => "persistence";
    private string? _persistence;
    
    protected override IEnumerable<string> EnumerateGlobals(Lua lua)
    {
        using var persistenceTable = lua.CreateTable();
        persistenceTable.SetValue("get", GetPersistence);
        persistenceTable.SetValue("set", SetPersistence);

        yield return lua.SetStringGlobal("persistence", persistenceTable);
    }

    public async ValueTask<string?> GetPersistence()
    {
        if (_persistence is not null)
            return _persistence;

        await using var scope = Context.CommandContext.Services.CreateAsyncScopeWithDatabase(out var db);
        var commandName = Context.CommandContext.GetMetadata<string>("command");
        var luaCommand = await db.LuaCommands.FirstAsync(x => x.GuildId == Context.CommandContext.GuildId && x.Name == commandName, CancellationToken);

        if (luaCommand.Persistence.Length == 0)
            return null;
            
        var value = Encoding.Default.GetString(luaCommand.Persistence.GZipDecompress());
        return _persistence = value;
    }

    public async Task<bool> SetPersistence(string value)
    {
        var bytes = Encoding.Default.GetBytes(value).GZipCompress();
        if (bytes.Length > MAX_PERSISTENCE_LENGTH)
            throw new InvalidOperationException($"Persistent data string cannot exceed {MAX_PERSISTENCE_LENGTH} bytes in compressed size.");
        
        try
        {
            await using var scope = Context.CommandContext.Bot.Services.CreateAsyncScopeWithDatabase(out var db);
            var commandName = Context.CommandContext.GetMetadata<string>("command");
            var luaCommand = await db.LuaCommands.FirstAsync(x => x.GuildId == Context.CommandContext.GuildId && x.Name == commandName, CancellationToken);
            luaCommand.Persistence = bytes;
            await db.SaveChangesAsync(CancellationToken);
            _persistence = value;
            return true;
        }
        catch
        {
            return false;
        }
    }
}