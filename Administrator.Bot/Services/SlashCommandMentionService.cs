using Disqord.Bot.Hosting;
using Qmmands;
using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Rest;
using Microsoft.Extensions.Logging;

namespace Administrator.Bot;

public sealed class SlashCommandMentionService : DiscordBotService
{
    private readonly Dictionary<string, ISlashCommand> _commandMap = new();

    public string GetMention(ICommand command)
    {
        var module = command.Module;
        var path = new[] { command.Name }.ToList();

        while (module?.CustomAttributes.OfType<SlashGroupAttribute>().SingleOrDefault() is { Alias: { } group })
        {
            path.Add(group);
            module = module.Parent;
        }

        path.Reverse();
        return GetMention(string.Join(' ', path));
    }

    public string GetMention(string commandPath)
    {
        var command = _commandMap[commandPath];
        return $"</{commandPath}:{command.Id}>";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Bot.WaitUntilReadyAsync(stoppingToken);

        var commands = await Bot.FetchGlobalApplicationCommandsAsync(Bot.CurrentUser.Id, cancellationToken: stoppingToken);

        foreach (var command in commands.OfType<ISlashCommand>())
        {
            foreach (var path in EnumeratePaths(command))
            {
                _commandMap[path] = command;
            }
        }
        
        Logger.LogInformation("Currently storing {Count} slash command mention mappings.", _commandMap.Count);

        static IEnumerable<string> EnumeratePaths(ISlashCommand command)
        {
            var commandName = command.Name;
            var subCommandGroups = new Dictionary<string, string[]>();

            if (!command.Options.Any(x => x.Type is SlashCommandOptionType.SubcommandGroup or SlashCommandOptionType.Subcommand))
                yield return commandName;

            foreach (var option in command.Options)
            {
                switch (option.Type)
                {
                    case SlashCommandOptionType.Subcommand:
                        subCommandGroups[option.Name] = Array.Empty<string>();
                        break;
                    case SlashCommandOptionType.SubcommandGroup:
                        subCommandGroups[option.Name] =
                            option.Options.Where(x => x.Type is SlashCommandOptionType.Subcommand).Select(x => x.Name).ToArray();
                        break;
                }
            }

            foreach (var (subCommandGroup, subcommands) in subCommandGroups)
            {
                if (subcommands.Length == 0)
                    yield return $"{commandName} {subCommandGroup}";
                
                foreach (var subCommand in subcommands)
                {
                    yield return $"{commandName} {subCommandGroup} {subCommand}";
                }
            }
        }
    }
}