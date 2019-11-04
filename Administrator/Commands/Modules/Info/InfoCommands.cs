using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Extensions;
using Administrator.Services;
using Discord;
using Qmmands;

namespace Administrator.Commands.Modules.Info
{
    [Name("Info")]
    public sealed class InfoCommands : AdminModuleBase
    {
        public CommandService Commands { get; set; }

        public PaginationService Pagination { get; set; }

        public ConfigurationService Config { get; set; }

        [Command("modules")]
        public AdminCommandResult GetModules()
        {
            var builder = new StringBuilder();

            foreach (var module in Commands.TopLevelModules.OrderBy(x => x.Name))
            {
                builder.AppendLine(Format.Bold(Format.Code(module.Name)))
                    .AppendLine(Localize($"info_modules_{module.Name.ToLower()}"))
                    .AppendLine();
            }

            return CommandSuccess(embed: new EmbedBuilder()
                .WithSuccessColor()
                .WithTitle(Localize("info_modules_title"))
                .WithDescription(builder.ToString())
                .Build());
        }

        [Command("commands", "module"), RunMode(RunMode.Parallel)]
        public async ValueTask<AdminCommandResult> GetCommandsAsync(Module module)
        {
            var commands = GetAllCommands(module);
            var groups = commands.GroupBy(x => x.FullAliases[0]).ToList();

            var pages = DefaultPaginator.GeneratePages(groups, lineFunc: group => new StringBuilder()
                    .Append(Format.Bold(FormatCommands(group)))
                    .AppendLine(Localize($"info_command_{group.Key.Replace(' ', '_')}")).ToString(),
                embedFunc: builder => builder.WithSuccessColor()
                    .WithTitle(Localize("info_module_commands", Format.Code(module.Name))));
            /*
            var pages = DefaultPaginator.GeneratePages(groups, 15, group => new EmbedFieldBuilder()
                //    .WithName(new StringBuilder(Config.DefaultPrefix)
                //        .AppendJoin($"\n{Config.DefaultPrefix}", group.First().FullAliases).ToString())
                .WithName(FormatCommands(group))
                .WithValue(Localize($"info_command_{group.Key.Replace(' ', '_')}")), 
                embedFunc: builder => builder.WithSuccessColor()
                    .WithTitle(Localize("info_module_commands", Format.Code(module.Name))));
            */

            if (pages.Count > 1)
            {
                var message = await Pagination.SendPaginatorAsync(Context.Channel, pages[0]);
                await using var paginator = new DefaultPaginator(message, pages, 0, Pagination);
                await paginator.WaitForExpiryAsync();
                return CommandSuccess();
            }

            return CommandSuccess(embed: pages[0].Embed);

            static List<Command> GetAllCommands(Module innerModule)
            {
                var innerCommands = innerModule.Commands.ToList();

                foreach (var submodule in innerModule.Submodules)
                {
                    innerCommands.AddRange(GetAllCommands(submodule));
                }

                return innerCommands;
            }
        }

        [Command("help")]
        public AdminCommandResult GetCommandHelp([Remainder] string commandName)
        {
            var matches = Commands.GetAllCommands()
                .Where(x => x.FullAliases.Contains(commandName, StringComparer.OrdinalIgnoreCase)).ToList();

            if (matches.Count == 0)
                return CommandErrorLocalized("info_help_notfound");

            /*
            if (matches.Count > 1) // TODO: is this even possible
                return CommandErrorLocalized("info_help_multiple");
            */

            var command = matches[0];
            return CommandSuccess(embed: new EmbedBuilder()
                .WithSuccessColor()
                .WithDescription(new StringBuilder()
                    .Append(Format.Bold(FormatCommands(matches)))
                    .AppendLine(Localize($"info_command_{command.FullAliases[0].Replace(' ', '_')}")).AppendLine()
                    .ToString())
                .WithFooter(Localize("info_module_reference", command.Module.Name))
                .Build());
        }

        private string FormatCommands(IEnumerable<Command> group)
        {
            var builder = new StringBuilder();
            foreach (var command in group.OrderBy(x => x.Parameters.Count))
            {
                builder.Append($"`{Config.DefaultPrefix}");
                var list = new List<string>();

                var topModule = command.Module;
                while (topModule is { })
                {
                    if (topModule.Aliases.Count > 0)
                    {
                        list.Add(topModule.Aliases.Count > 1
                            ? $"({string.Join('|', topModule.Aliases)})"
                            : topModule.Aliases[0]);
                    }
                    else if (topModule.Attributes.OfType<GroupAttribute>().FirstOrDefault() is { } groupAttribute)
                    {
                        list.Add(groupAttribute.Aliases.Length > 1
                                 ? $"({string.Join('|', groupAttribute.Aliases)})"
                                 : groupAttribute.Aliases[0]);
                    }

                    topModule = topModule.Parent;
                }

                for (var i = list.Count - 1; i >= 0; i--)
                {
                    builder.Append($"{list[i]} ");
                }

                var aliases = command.Aliases.ToList();
                if (aliases.Contains(""))
                {
                    var temp = builder.ToString();
                    builder.AppendLine($" {command.FormatArguments()}`")
                        .Append(temp);

                    aliases.Remove("");
                }

                if (aliases.Count > 0)
                {
                    builder.Append(aliases.Count > 1
                        ? $"({string.Join('|', aliases)})"
                        : aliases[0]);
                }

                builder.AppendLine($" {command.FormatArguments()}`");

                //builder.AppendLine(Format.Code(Config.DefaultPrefix + group.Key +
                //                               command.FormatArguments()));
            }

            return builder.ToString();
        }
    }
}