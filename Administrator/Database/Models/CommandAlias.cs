using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Administrator.Commands;
using Administrator.Extensions;

namespace Administrator.Database
{
    public sealed class CommandAlias
    {
        private CommandAlias()
        { }

        public CommandAlias(ulong guildId, string alias, string command)
        {
            GuildId = guildId;
            Alias = alias.ToLowerInvariant();
            Command = command;
        }

        public ulong GuildId { get; set; }

        public string Alias { get; set; }

        public string Command { get; set; }

        public async Task<string> ReplaceCommandAsync(string command, AdminCommandContext context)
        {
            var formattedCommand = await Command.FormatPlaceHoldersAsync(context);
            return new Regex($@"{Alias}\b", RegexOptions.IgnoreCase)
                .Replace(command, Replace, 1);

            string Replace(Match match)
                => formattedCommand;
        }
    }
}