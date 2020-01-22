using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Administrator.Commands;
using Administrator.Database;
using Qmmands;

namespace Administrator.Services
{
    public sealed class CommandCooldownService : Service,
        IHandler<CommandExecutedEventArgs>
    {
        public CommandCooldownService(IServiceProvider provider)
            : base(provider)
        { }

        public async Task HandleAsync(CommandExecutedEventArgs args)
        {
            var result = (AdminCommandResult) args.Result;
            if (!result.IsSuccessful) return; // Only put the user on cooldown if the command was successful

            var context = (AdminCommandContext) args.Context;
            var now = DateTimeOffset.UtcNow;
            var commandName = context.Command.FullAliases[0].ToLowerInvariant();
            var per = context.Command.Attributes.OfType<CooldownAttribute>().First().Per;
            if (!context.IsPrivate)
            {
                using var ctx = new AdminDatabaseContext(_provider);
                if (await ctx.Cooldowns.FindAsync(context.Guild.Id.RawValue, commandName) is { } cooldown)
                {
                    per = cooldown.Cooldown;
                }
            }

            if (per > AdminCooldownProvider.MinimumMemoryCooldown)
            {
                using var ctx = new AdminDatabaseContext(_provider);
                ctx.CooldownData.Add(new CooldownData(context.Guild?.Id ?? 0, context.User.Id, commandName, now));
                await ctx.SaveChangesAsync();
                return;
            }

            if (AdminCooldownProvider.CooldownBuckets.TryGetValue((context.Guild?.Id ?? 0, context.User.Id),
                out var commandMappings))
            {
                commandMappings[commandName] = now;
                return;
            }

            AdminCooldownProvider.CooldownBuckets[(context.Guild?.Id ?? 0, context.User.Id)] =
                new Dictionary<string, DateTimeOffset> {[commandName] = now};
        }
    }
}