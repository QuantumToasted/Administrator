﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Extensions;
using Administrator.Services;
using Disqord;
using Humanizer.Localisation;
using Qmmands;
using Module = Qmmands.Module;

namespace Administrator.Commands
{
    [Name("Info")]
    public sealed class InfoCommands : AdminModuleBase
    {
        public CommandService Commands { get; set; }

        public PaginationService Pagination { get; set; }

        public ConfigurationService Config { get; set; }

        public StatsService Stats { get; set; }

        [Command("modules")]
        [IgnoresExtraArguments]
        public AdminCommandResult GetModules()
        {
            var builder = new StringBuilder();

            foreach (var module in Commands.TopLevelModules.OrderBy(x => x.Name))
            {
                if (module.Checks.OfType<RequireOwnerAttribute>().Any() && 
                    !Config.OwnerIds.Contains(Context.User.Id)) continue;

                builder.AppendLine(Markdown.Bold(Markdown.Code(module.Name)))
                    .AppendLine(Localize($"info_modules_{module.Name.ToLower()}"))
                    .AppendLine();
            }

            return CommandSuccess(embed: new LocalEmbedBuilder()
                .WithSuccessColor()
                .WithTitle(Localize("info_modules_title"))
                .WithDescription(builder.ToString())
                .Build());
        }

        [Command("commands", "module")]
        public async ValueTask<AdminCommandResult> GetCommandsAsync(Module module)
        {
            var commands = CommandUtilities.EnumerateAllCommands(module);
            var groups = commands.GroupBy(x => x.FullAliases[0]).ToList();

            var pages = DefaultPaginator.GeneratePages(groups, lineFunc: group => new StringBuilder()
                    .Append(Markdown.Bold(FormatCommands(group)))
                    .AppendLine(Localize($"info_command_{group.Key.Replace(' ', '_')}")).ToString(),
                builder: new LocalEmbedBuilder().WithSuccessColor()
                    .WithTitle(Localize("info_module_commands", Markdown.Code(module.Name))));
            /*
            var pages = DefaultPaginator.GeneratePages(groups, 15, group => new EmbedFieldBuilder()
                //    .WithName(new StringBuilder(Config.DefaultPrefix)
                //        .AppendJoin($"\n{Config.DefaultPrefix}", group.First().FullAliases).Tag)
                .WithName(FormatCommands(group))
                .WithValue(Localize($"info_command_{group.Key.Replace(' ', '_')}")), 
                embedFunc: builder => builder.WithSuccessColor()
                    .WithTitle(Localize("info_module_commands", Format.Code(module.Name))));
            */

            if (pages.Count > 1)
            {
                await Pagination.SendPaginatorAsync(Context.Channel, new DefaultPaginator(pages, 0), pages[0]);
                return CommandSuccess();
            }

            return CommandSuccess(embed: pages[0].Embed);
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
            return CommandSuccess(embed: new LocalEmbedBuilder()
                .WithSuccessColor()
                .WithDescription(new StringBuilder()
                    .Append(Markdown.Bold(FormatCommands(matches)))
                    .AppendLine(Localize($"info_command_{command.FullAliases[0].Replace(' ', '_')}")).AppendLine()
                    .ToString())
                .WithFooter(Localize("info_module_reference", command.Module.Name))
                .Build());
        }

        [Command("stats")]
        [IgnoresExtraArguments]
        public async ValueTask<AdminCommandResult> GetBotStatsAsync()
        {
            var app = await Context.Client.GetCurrentApplicationAsync();
            return CommandSuccess(embed: new LocalEmbedBuilder()
                .WithSuccessColor()
                .WithAuthor(
                    Localize("info_stats_author", Context.Client.CurrentUser.Tag,
                        Stats.BuildDate.ToString("d", Context.Language.Culture)),
                    Context.Client.CurrentUser.GetAvatarUrl())
                .AddField(Localize("info_stats_uptime"),
                    string.Join('\n',
                        Stats.Uptime.HumanizeFormatted(Localization, Context.Language, TimeUnit.Second)
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => x.Trim())), true)
                .AddField(Localize("info_stats_presence"),
                    Localize("info_stats_presence_text", Stats.TotalGuilds, Stats.TotalTextChannels,
                        Stats.TotalVoiceChannels, Stats.TotalMembers), true)
                .AddField(Localize("info_stats_commands"), Stats.CommandsExecuted, true)
                .AddField(Localize("info_stats_messages"),
                    Localize("info_stats_messages_text", Stats.MessagesReceived,
                        (Stats.MessagesReceived / Stats.Uptime.TotalSeconds).ToString("F")), true)
                .AddField(Localize("info_stats_memory"),
                    Localize("info_stats_megabytes", (Stats.MemoryUsage / 1_000_000D).ToString("F")), true)
                .AddField(Localize("info_stats_nerds"), FormatAssemblies(Stats.CustomAssemblies).TrimTo(1024, true))
                .WithFooter(Localize("info_stats_created", app.Owner.Tag), app.Owner.GetAvatarUrl())
                .Build());

            static string FormatAssemblies(IEnumerable<Assembly> assemblies)
            {
                var builder = new StringBuilder();
                foreach (var assemblyName in assemblies.Select(x => x.GetName()))
                {
                    if (assemblyName.Version is { } version)
                    {
                        builder.AppendLine($"{assemblyName.Name} v{version}");
                    }
                }

                return builder.ToString();
            }
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
                        if (topModule.Aliases.Count > 1)
                        {
                            list.Add($"({string.Join('|', topModule.Aliases)})");
                        }
                        else if (!string.IsNullOrWhiteSpace(topModule.Aliases[0]))
                        {
                            list.Add(topModule.Aliases[0]);
                        }
                        /*
                        list.Add(topModule.Aliases.Count > 1
                            ? $"({string.Join('|', topModule.Aliases)})"
                            : topModule.Aliases[0]);
                        */
                    }
                    else if (topModule.Attributes.OfType<GroupAttribute>().FirstOrDefault() is { } groupAttribute)
                    {
                        if (groupAttribute.Aliases.Length > 1)
                        {
                            list.Add($"({string.Join('|', groupAttribute.Aliases)})");
                        }
                        else if (!string.IsNullOrWhiteSpace(groupAttribute.Aliases[0]))
                        {
                            list.Add(groupAttribute.Aliases[0]);
                        }

                        /*
                        list.Add(groupAttribute.Aliases.Length > 1
                                 ? $"({string.Join('|', groupAttribute.Aliases)})"
                                 : groupAttribute.Aliases[0]);
                        */
                    }

                    topModule = topModule.Parent;
                }

                var aliases = command.Aliases.ToList();

                for (var i = list.Count - 1; i >= 0; i--)
                {
                    var entry = list[i];
                    builder.Append(entry)
                        .Append(' ');
                }


                if (aliases.Contains(""))
                {
                    var temp = builder.ToString();
                    builder.AppendLine($"{command.FormatArguments()}`")
                        .Append(temp);

                    aliases.Remove("");
                }

                if (aliases.Count > 0)
                {
                    builder.Append(aliases.Count > 1
                        ? $"({string.Join('|', aliases)})"
                        : aliases[0]);
                }

                builder.AppendLine($"{command.FormatArguments()}`");
            }

            return string.Join('\n', builder.ToString().Split('\n').Distinct())
                .Replace("  ", " ").Replace(" `", "`");
        }
    }
}