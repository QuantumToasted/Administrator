using System.Collections.Concurrent;
using Disqord;
using Disqord.AuditLogs;
using Disqord.Bot.Hosting;
using Disqord.Gateway;

namespace Administrator.Bot;

public sealed class AuditLogService : DiscordBotService
{
    private readonly ConcurrentDictionary<Snowflake, ConcurrentDictionary<Snowflake, IAuditLog>> _auditLogs = new();

    public ConcurrentDictionary<Snowflake, IAuditLog> GetAllAuditLogs(Snowflake guildId)
        => _auditLogs.GetOrAdd(guildId, _ => new ConcurrentDictionary<Snowflake, IAuditLog>());

    public IAuditLog? GetAuditLog(Snowflake guildId, Func<IAuditLog, bool> func)
        => GetAuditLog<IAuditLog>(guildId, func);

    public TAuditLog? GetAuditLog<TAuditLog>(Snowflake guildId, Func<TAuditLog, bool> func)
        where TAuditLog : class, IAuditLog
    {
        var auditLogs = GetAllAuditLogs(guildId);
        return auditLogs.Values.OfType<TAuditLog>().OrderByDescending(x => x.Id).FirstOrDefault(func);
    }

    protected override ValueTask OnAuditLogCreated(AuditLogCreatedEventArgs e)
    {
        var dict = _auditLogs.GetOrAdd(e.GuildId, _ => new ConcurrentDictionary<Snowflake, IAuditLog>());
        dict[e.AuditLog.Id] = e.AuditLog;
        return ValueTask.CompletedTask;
    }
}