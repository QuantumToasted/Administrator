using Disqord.Bot.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Administrator.Bot;

public sealed class DbMigrationCheckService : DiscordBotService
{
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        var migrations = await db.Database.GetPendingMigrationsAsync(cancellationToken);

        if (migrations.Any())
        {
            Logger.LogCritical(@"╔══════════════════════════════════════════════╗");
            Logger.LogCritical(@"║ PENDING MIGRATION(S) DETECTED! STOPPING BOT. ║");
            Logger.LogCritical(@"╚══════════════════════════════════════════════╝");
            
            Environment.Exit(-1);
        }
    }
}