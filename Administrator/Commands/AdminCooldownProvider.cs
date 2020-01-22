using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Administrator.Services;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class AdminCooldownProvider : ICooldownProvider
    {
        public static readonly TimeSpan MinimumMemoryCooldown = TimeSpan.FromHours(1);

        public static readonly IDictionary<(ulong GuildId, ulong UserId), IDictionary<string, DateTimeOffset>> CooldownBuckets =
            new Dictionary<(ulong GuildId, ulong UserId), IDictionary<string, DateTimeOffset>>();

        public async ValueTask<CooldownResult> CheckCooldownAsync(CooldownAttribute cooldown, CommandContext ctx)
        {
            var per = cooldown.Per;
            var context = (AdminCommandContext) ctx;
            var logging = context.ServiceProvider.GetRequiredService<LoggingService>();
            var now = DateTimeOffset.UtcNow;
            var commandName = context.Command.FullAliases[0].ToLowerInvariant();

            if (!context.IsPrivate &&
                await context.Database.Cooldowns.FindAsync(context.Guild.Id.RawValue, commandName) is { } cd)
            {
                per = cd.Cooldown;
            }

            TimeSpan elapsed;
            if (per > MinimumMemoryCooldown)
            {
                if (await context.Database.CooldownData.FindAsync(context.Guild?.Id.RawValue ?? 0, 
                    context.User.Id, commandName) is { } data)
                {
                    elapsed = now - data.LastRun;
                    if (elapsed > per)
                    {
                        context.Database.CooldownData.Remove(data);
                        await context.Database.SaveChangesAsync();
                        return CooldownResult.NotOnCooldown;
                    }

                    return CooldownResult.OnCooldown(per - elapsed);
                }

                return CooldownResult.NotOnCooldown;
            }

            if (!CooldownBuckets.TryGetValue((context.Guild?.Id ?? 0, context.User.Id), out var commandMapping) ||
                !commandMapping.TryGetValue(commandName, out var lastRun))
            {
                return CooldownResult.NotOnCooldown;
            }

            elapsed = now - lastRun;
            if (elapsed > per)
            {
                commandMapping.Remove(commandName);
                return CooldownResult.NotOnCooldown;
            }

            return CooldownResult.OnCooldown(per - elapsed);
        }
    }
}