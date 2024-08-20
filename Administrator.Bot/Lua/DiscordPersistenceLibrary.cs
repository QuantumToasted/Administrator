using System.Text;
using Administrator.Core;
using Disqord.Bot.Commands.Interaction;
using Laylua;
using Microsoft.EntityFrameworkCore;
using Qommon.Metadata;

namespace Administrator.Bot;

public sealed class DiscordPersistenceLibrary(IDiscordInteractionCommandContext context, CancellationToken cancellationToken) : DiscordLuaLibraryBase(cancellationToken)
{
    private const int MAX_PERSISTENCE_LENGTH = 1_000_000;
    
    public override string Name => "persistence";
    private Lazy<string?>? _persistence;
    
    protected override IEnumerable<string> RegisterGlobals(Lua lua)
    {
        using var persistenceTable = lua.CreateTable();
        persistenceTable.SetValue("get", GetPersistence);
        persistenceTable.SetValue("set", SetPersistence);

        yield return lua.SetStringGlobal("persistence", persistenceTable);
    }

    public string? GetPersistence()
    {
        _persistence ??= new Lazy<string?>(() => RunWait(async ct =>
        {
            await using var scope = context.Bot.Services.CreateAsyncScopeWithDatabase(out var db);
            var commandName = context.GetMetadata<string>("command");
            var luaCommand = await db.LuaCommands.FirstAsync(x => x.GuildId == context.GuildId && x.Name == commandName, ct);

            if (luaCommand.Persistence.Length == 0)
                return null;
            
            var value = Encoding.Default.GetString(luaCommand.Persistence.GZipDecompress());
            return value;
        }, true));

        return _persistence.Value;
    }

    public bool SetPersistence(string value)
    {
        if (value.Length > MAX_PERSISTENCE_LENGTH)
            throw new InvalidOperationException($"Persistent data string cannot exceed {MAX_PERSISTENCE_LENGTH} characters.");
        
        return RunWait(async ct =>
        {
            try
            {
                await using var scope = context.Bot.Services.CreateAsyncScopeWithDatabase(out var db);
                var commandName = context.GetMetadata<string>("command");
                var luaCommand = await db.LuaCommands.FirstAsync(x => x.GuildId == context.GuildId && x.Name == commandName, ct);
                luaCommand.Persistence = Encoding.Default.GetBytes(value).GZipCompress();
                await db.SaveChangesAsync(ct);
                _persistence = new Lazy<string?>(value);
                return true;
            }
            catch
            {
                return false;
            }

        }, true);
    }
}