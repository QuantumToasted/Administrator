using System.Collections.Concurrent;
using Disqord;
using Disqord.AuditLogs;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Disqord.Utilities.Threading;
using Microsoft.Extensions.Logging;

namespace Administrator.Bot;

public sealed class AuditLogService : DiscordBotService
{
    private readonly ConcurrentDictionary<Snowflake, ConcurrentDictionary<Snowflake, IAuditLog>> _auditLogs = new();

    private readonly ConcurrentDictionary<TaskCompletionSource<IAuditLog>, WaiterState> _waiters = new();
    
    public ConcurrentDictionary<Snowflake, IAuditLog> GetAllAuditLogs(Snowflake guildId)
        => _auditLogs.GetOrAdd(guildId, _ => new ConcurrentDictionary<Snowflake, IAuditLog>());
    
    /*

    public IAuditLog? GetAuditLog(Snowflake guildId, Func<IAuditLog, bool> func)
        => GetAuditLog<IAuditLog>(guildId, func);

    public TAuditLog? GetAuditLog<TAuditLog>(Snowflake guildId, Func<TAuditLog, bool> func)
        where TAuditLog : class, IAuditLog
    {
        var auditLogs = GetAllAuditLogs(guildId);
        var filteredLogs = auditLogs.Values.OfType<TAuditLog>();
        
        return filteredLogs.OrderByDescending(x => x.Id).FirstOrDefault(func);
    }
    */

    public async Task<TAuditLog?> WaitForAuditLogAsync<TAuditLog>(Snowflake guildId, Func<TAuditLog, bool> func, TimeSpan? timeout = null)
        where TAuditLog : class, IAuditLog
    {
        var existingAuditLogs = GetAllAuditLogs(guildId);
        if (existingAuditLogs.Values.FirstOrDefault(log => log is TAuditLog typedLog && func.Invoke(typedLog)) is TAuditLog existingLog)
            return existingLog;
        
        timeout ??= TimeSpan.FromSeconds(1);
        using var cts = Cts.Linked(Bot.StoppingToken);
        cts.CancelAfter(timeout.Value);

        var state = new WaiterState(guildId, log => log is TAuditLog typedLog && func.Invoke(typedLog), cts.Token);
        var tcs = new TaskCompletionSource<IAuditLog>();
        _waiters.TryAdd(tcs, state);

        try
        {
            var log = (TAuditLog)await tcs.Task;
            return log;
        }
        catch (OperationCanceledException)
        {
            Logger.LogDebug("Timeout exceeded waiting for audit log.");
        }
        finally
        {
            _waiters.Remove(tcs, out _);
        }
        
        return null;
    }

    public async Task<IAuditLog?> WaitForAuditLogAsync(Snowflake guildId, Func<IAuditLog, bool> func, TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(1);
        using var cts = Cts.Linked(Bot.StoppingToken);
        cts.CancelAfter(timeout.Value);

        var state = new WaiterState(guildId, func, cts.Token);
        var tcs = new TaskCompletionSource<IAuditLog>();
        _waiters.TryAdd(tcs, state);

        try
        {
            var log = await tcs.Task;
            return log;
        }
        catch (OperationCanceledException)
        { }
        finally
        {
            _waiters.Remove(tcs, out _);
        }
        
        return null;
    }

    protected override ValueTask OnAuditLogCreated(AuditLogCreatedEventArgs e)
    {
        var dict = _auditLogs.GetOrAdd(e.GuildId, _ => new ConcurrentDictionary<Snowflake, IAuditLog>());
        dict[e.AuditLog.Id] = e.AuditLog;

        foreach (var (waiter, state) in _waiters)
        {
            if (state.CancellationToken.IsCancellationRequested)
            {
                waiter.SetCanceled(state.CancellationToken);
                continue;
            }

            if (e.AuditLog.GuildId == state.GuildId && state.Func.Invoke(e.AuditLog))
            {
                waiter.SetResult(e.AuditLog);
            }
        }
        
        return ValueTask.CompletedTask;
    }

    private record WaiterState(Snowflake GuildId, Func<IAuditLog, bool> Func, CancellationToken CancellationToken);
}