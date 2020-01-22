using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Services;
using Disqord;
using Disqord.Rest;
using Qmmands;

namespace Administrator.Commands.Prune
{
    [Name("Prune")]
    [Group("prune", "purge")]
    [RequireBotPermissions(Permission.ManageMessages, Group = "bot")]
    [RequireBotPermissions(Permission.ManageMessages, false, Group = "bot")]
    [RequireUserPermissions(Permission.ManageMessages, Group = "user")]
    [RequireUserPermissions(Permission.ManageMessages, false, Group = "user")]
    public sealed class PruneCommands : AdminModuleBase
    {
        public LoggingService Logging { get; set; }

        private CachedTextChannel Channel => (CachedTextChannel) Context.Channel;

        [Command, RunMode(RunMode.Parallel)]
        public ValueTask<AdminCommandResult> PruneMessages([MustBe(Operator.GreaterThan, 0)]
            [MustBe(Operator.LessThan, 10000)] int limit)
            => PruneMessagesAsync(limit);

        [Command, RunMode(RunMode.Parallel)]
        public ValueTask<AdminCommandResult> PruneMessages([MustBe(Operator.GreaterThan, 0)]
            [MustBe(Operator.LessThan, 10000)] int limit,
            RetrievalDirection direction,
            ulong messageId)
        {
            if (direction == RetrievalDirection.Around)
                return CommandSuccess(); // silent failure

            return direction == RetrievalDirection.Before
                ? PruneMessagesAsync(limit, message => message.Id < messageId)
                : PruneMessagesAsync(limit, message => message.Id > messageId);
        }

        [Command, RunMode(RunMode.Parallel)]
        public ValueTask<AdminCommandResult> PruneMessages([MustBe(Operator.GreaterThan, 0)]
            [MustBe(Operator.LessThan, 10000)] int limit,
            [Remainder] CachedMember member)
            => PruneMessagesAsync(limit, message => message.Author.Id == member.Id);

        [Command, RunMode(RunMode.Parallel)]
        public ValueTask<AdminCommandResult> PruneMessages([MustBe(Operator.GreaterThan, 0)]
            [MustBe(Operator.LessThan, 10000)] int limit, ulong userId)
            => PruneMessagesAsync(limit, message => message.Author.Id == userId);

        [Command("text"), RunMode(RunMode.Parallel)]
        public ValueTask<AdminCommandResult> PruneMessages([MustBe(Operator.GreaterThan, 0)]
            [MustBe(Operator.LessThan, 10000)] int limit,
            [Remainder] string text)
            => PruneMessagesAsync(limit,
                message => message.Content?.Equals(text, StringComparison.OrdinalIgnoreCase) == true);

        private async ValueTask<AdminCommandResult> PruneMessagesAsync(int limit, Func<RestMessage, bool> func = null)
        {
            var bin = new List<Snowflake>();
            var now = DateTimeOffset.UtcNow;

            await foreach (var messages in Channel.GetMessagesEnumerable(int.MaxValue, startFromId: Context.Message.Id))
            {
                await Logging.LogDebugAsync($"Got {messages.Count} message(s).", "PruneCommands");

                var filteredMessages = messages.Where(x => now - x.Id.CreatedAt < TimeSpan.FromDays(14));

                var previousCount = bin.Count;
                if (func is { })
                    filteredMessages = filteredMessages.Where(func);

                await Logging.LogDebugAsync($"Added {filteredMessages.Count()} messages to the bin.", "PruneCommands");
                bin.AddRange(filteredMessages.Select(x => x.Id));
                await Logging.LogDebugAsync($"Bin size {bin.Count}/{limit}", "PruneCommands");

                // break if no messages were found, or the bin is as full as it needs to be
                if (bin.Count == previousCount || bin.Count >= limit)
                    break;
            }

            if (bin.Count > 0)
                await Channel.DeleteMessagesAsync(bin.Take(limit));

            return CommandSuccess();

            /*await using var enumerator = Channel.GetMessagesEnumerator(10000, startFromId: Context.Message.Id);
            var bin = new List<Snowflake>();
            var now = DateTimeOffset.UtcNow;

            while (await enumerator.MoveNextAsync())
            {
                await Logging.LogDebugAsync($"Got {enumerator.Current.Count} message(s).", "PruneCommands");

                var messages = enumerator.Current.Where(x => now - x.Id.CreatedAt < TimeSpan.FromDays(14));
                var previousCount = bin.Count;
                if (func is { })
                    messages = messages.Where(func);

                await Logging.LogDebugAsync($"Added {messages.Count()} messages to the bin.", "PruneCommands");
                bin.AddRange(messages.Select(x => x.Id));
                await Logging.LogDebugAsync($"Bin size {bin.Count}/{limit}", "PruneCommands");

                // break if no messages were found, or the bin is as full as it needs to be
                if (bin.Count == previousCount || bin.Count >= limit)
                    break;
            }

            if (bin.Count > 0)
                await Channel.DeleteMessagesAsync(bin.Take(limit));

            return CommandSuccess();
            */
        }
    }
}