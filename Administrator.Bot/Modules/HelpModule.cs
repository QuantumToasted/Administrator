using Administrator.Core;
using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Disqord.Rest;
using Microsoft.Extensions.Options;
using Qmmands;

namespace Administrator.Bot;

public sealed class HelpModule(IOptions<AdministratorHelpConfiguration> options, EmojiService emojis, SlashCommandMentionService mentions,
    AutoCompleteService autoComplete) : DiscordApplicationModuleBase
{
    private static IApplication? _application;
    private readonly AdministratorHelpConfiguration _config = options.Value;
    
    [SlashCommand("help")]
    [Description("Displays help information related to the bot.")]
    public async Task<IResult> HelpAsync(/*string? topic = null*/)
    {
        // TODO: topic
        _application ??= await Bot.FetchCurrentApplicationAsync();
        var owner = _application.Team is { } team
            ? await Bot.GetOrFetchUserAsync(team.OwnerId)
            : _application.Owner;

        var helpIcon = emojis.Names["interrobang"];
        var supportServerButton = new LocalLinkButtonComponent()
            .WithUrl($"https://discord.gg/{_config.SupportGuildInviteCode}")
            .WithEmoji(helpIcon)
            .WithLabel("Support Server");
        
        var wikiIcon = emojis.Names["book"];
        var wikiButton = new LocalLinkButtonComponent()
            .WithUrl(_config.WikiUrl)
            .WithEmoji(wikiIcon)
            .WithLabel("Wiki");

        var embed = new LocalEmbed()
            .WithUnusualColor()
            .WithAuthor($"Hello there, {Context.Author.Tag}. My name is Helen, but please, just call me the Administrator.", Bot.CurrentUser.GetAvatarUrl())
            .WithDescription("I was originally designed exclusively for the TF2 Community Discord server.\n" +
                             "I've since been overhauled uncountable times and am now a public bot!\n\n" +
                             "Below you can find several buttons that may lead you to the answer to your question, or the solution to your problem.")
            .WithFooter($"Made with {emojis.Names["heart"]} by {owner!.Tag}", owner.GetAvatarUrl());
        
        var response = new LocalInteractionMessageResponse()
            .WithIsEphemeral(Context.GuildId.HasValue)
            .AddEmbed(embed)
            .AddComponent(LocalComponent.Row(supportServerButton, wikiButton));

        return Response(response);
    }

    [SlashCommand("mention-command")]
    [Description("Provide's a command's mention to include it in other messages.")]
    public IResult MentionCommand(
        [Description("The full name of the command.")]
            string commandName)
    {
        var mention = mentions.GetMention(commandName);
        if (mention is null)
            return Response("No command could be found with that name!").AsEphemeral();

        if (mention.StartsWith('`')) // code
            return Response("A slash command mention could not be generated for that command!").AsEphemeral();

        return Response($"{mention} ({Markdown.Code(mention)})");
    }

    [AutoComplete("mention-command")]
    public void AutoCompleteCommands(AutoComplete<string> commandName)
    {
        var commands = Bot.Commands.EnumerateApplicationModules()
            .SelectMany(x => x.Commands)
            .Where(x =>
                x.Type is ApplicationCommandType.Slash &&
                !CommandUtilities.EnumerateAllChecks(x).OfType<RequireBotOwnerAttribute>().Any() &&
                !CommandUtilities.EnumerateAllChecks(x).OfType<RequireGuildAttribute>().Any(y => y.Id.HasValue))
            .ToList();
        
        autoComplete.AutoComplete(commandName, commands);
    }
}