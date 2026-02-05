using Disqord.Bot.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Administrator.Bot;

public sealed class MigrationCheckService : DiscordBotService
{
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        var migrations = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();

        if (migrations.Length > 0)
        {
            Logger.LogInformation("Applying {Count} migration(s): {Migrations}", migrations.Length, migrations);
            await db.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            Logger.LogInformation("No pending migrations!");
        }
    }
}