using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Administrator.Commands;
using Administrator.Database;
using Disqord;
using Humanizer;
using Qmmands;

namespace Administrator.Extensions
{
    public static class CommandExtensions
    {
        private static readonly HashSet<(Snowflake? GuildId, Snowflake UserId, string CommandName)> ExecutingCommands =
            new HashSet<(Snowflake? GuildId, Snowflake UserId, string CommandName)>();

        public static bool IsExecuting(this Command command, AdminCommandContext context)
            => command.RunMode == RunMode.Parallel && ExecutingCommands.Contains((context.Guild?.Id, context.User.Id,
                   command.FullAliases[0].ToLowerInvariant()));

        public static void BeginExecution(this Command command, AdminCommandContext context)
        {
            if (command.RunMode != RunMode.Parallel) return;
            ExecutingCommands.Add((context.Guild?.Id, context.User.Id,
                command.FullAliases[0].ToLowerInvariant()));
        }

        public static void EndExecution(this Command command, AdminCommandContext context)
        {
            ExecutingCommands.Remove((context.Guild?.Id, context.User.Id,
                command.FullAliases[0].ToLowerInvariant()));
        }

        public static string FormatArguments(this Command command)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < command.Parameters.Count; i++)
            {
                var parameter = command.Parameters[i];
                builder.Append(' ')
                    .Append(parameter.IsOptional ? '[' : '<')
                    .Append(parameter.Name.Humanize().ToLower()) // TODO: possible localized parameters
                    .Append(parameter.IsRemainder ? "..." : string.Empty)
                    .Append(parameter.IsOptional ? ']' : '>')
                    .Append(parameter.IsMultiple ? "[]" : string.Empty);
            }

            return builder.ToString();
        }

        public static async Task<IResult> ExecuteAsync(this CommandService commands, CommandAlias alias, string command,
            AdminCommandContext context)
        {
            command = await alias.ReplaceCommandAsync(command, context);
            return await commands.ExecuteAsync(command, context);
        }
    }
}