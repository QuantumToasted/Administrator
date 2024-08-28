using System.Text;
using System.Text.RegularExpressions;
using Disqord;
using Humanizer;
using Laylua;
using Qommon;

namespace Administrator.Bot;

public sealed class LuaSlashCommand : ILuaModel<LuaSlashCommand>
{
    private static readonly Regex NameRegex = new(@"^[-_\p{L}\p{N}\p{Sc}]{1,32}$", RegexOptions.Compiled);
    
    public LuaSlashCommand(LuaTable table)
    {
        var name = table.GetValueOrDefault<string, string>("name");
        name = name?.Kebaberize();
        Guard.IsNotNullOrWhiteSpace(name);
        Guard.HasSizeLessThanOrEqualTo(name, Discord.Limits.ApplicationCommand.MaxNameLength);

        Name = name;
        
        var description = table.GetValueOrDefault<string, string>("description") ?? "No description.";
        Guard.IsNotNullOrWhiteSpace(description);
        Guard.HasSizeLessThanOrEqualTo(description, Discord.Limits.ApplicationCommand.MaxDescriptionLength);

        Description = description;

        if (table.TryGetValue<string, long>("permissions", out var permissions))
            Permissions = (Permissions)(ulong)permissions;

        if (table.TryGetValue<string, LuaTable>("options", out var options))
            Options = options;
    }
    
    public string Name { get; }
    
    public string Description { get; } 
    
    public Permissions? Permissions { get;}
    
    public LuaTable? Options { get; }
    
    public IEnumerable<LuaSlashCommandOption> GetOptions()
    {
        if (Options is null)
            yield break;
        
        foreach (var (key, value) in Options)
        {
            Guard.IsNotNull(key.Value);
            var name = Guard.IsOfType<string>(key.Value);
            Guard.IsNotNullOrWhiteSpace(name);
            Guard.HasSizeLessThanOrEqualTo(name, Discord.Limits.ApplicationCommand.Option.MaxNameLength);
                
            Guard.IsNotNull(value.Value);
            var table = Guard.IsOfType<LuaTable>(value.Value);

            yield return new LuaSlashCommandOption(name, table);
        }
    }

    public LocalEmbed ToDisplayEmbed()
    {
        var embed = new LocalEmbed()
            .WithUnusualColor()
            .WithTitle($"/{Name}")
            .WithDescription(Markdown.Italics(Description));

        foreach (var option in GetOptions())
        {
            var field = new LocalEmbedField()
                .WithName(option.Name);

            var valueBuilder = new StringBuilder()
                .AppendNewline($"Type: {option.Type.Humanize(LetterCasing.Sentence)}");

            if (!string.IsNullOrWhiteSpace(option.Description))
                valueBuilder.AppendNewline(Markdown.Italics(option.Description));

            field.WithValue(valueBuilder.ToString());
        }

        return embed;
    }

    public void Validate()
    {
        var totalLength = 0;
        
        Guard.IsNotNullOrWhiteSpace(Name);
        Guard.HasSizeLessThanOrEqualTo(Name, Discord.Limits.ApplicationCommand.MaxNameLength);
        Guard.IsTrue(NameRegex.IsMatch(Name));
        totalLength += Name.Length;
        
        Guard.IsNotNullOrWhiteSpace(Description);
        Guard.HasSizeLessThanOrEqualTo(Description, Discord.Limits.ApplicationCommand.MaxDescriptionLength);
        totalLength += Description.Length;

        foreach (var option in EnumerateAllOptions(GetOptions()))
        {
            Guard.IsNotNullOrWhiteSpace(option.Name);
            Guard.IsTrue(NameRegex.IsMatch(option.Name));
            totalLength += option.Name.Length;
            
            Guard.IsNotNullOrWhiteSpace(option.Description);
            totalLength += option.Description.Length;

            var subOptions = option.GetOptions().ToList();
            switch (option.Type)
            {
                case SlashCommandOptionType.Subcommand:
                {
                    if (EnumerateAllOptions(subOptions).Any(x => x.Type is SlashCommandOptionType.Subcommand or SlashCommandOptionType.SubcommandGroup))
                        Throw.FormatException($"Option \"{option.Name}\": \"subcommand\" type options cannot have nested \"subcommand\" or \"subcommandgroup\" type options.");
                    
                    continue;
                }
                case SlashCommandOptionType.SubcommandGroup:
                {
                    if (subOptions.Count == 0)
                        Throw.FormatException($"Option \"{option.Name}\": \"subcommandgroup\" type options MUST have top-level nested \"subcommand\" type options.");
                    
                    foreach (var subOption in subOptions)
                    {
                        if (subOption.Type is not SlashCommandOptionType.Subcommand)
                            Throw.FormatException($"Option \"{option.Name}\": \"subcommandgroup\" type options' top-level nested options must be of the type \"subcommand\".");
                    }
                    
                    break;
                }
                default:
                {
                    if (subOptions.Count > 0)
                        Throw.FormatException($"Option \"{option.Name}\": Only \"subcommand\" and \"subcommandgroup\" type options can have nested options.");
                    
                    continue;
                }
            }
        }
        
        Guard.IsLessThanOrEqualTo(totalLength, 8000);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not LuaSlashCommand other)
            return false;

        if (other.Name != Name)
            return false;

        if (other.Description != Description)
            return false;

        if (other.Permissions != Permissions)
            return false;

        var otherOptions = EnumerateAllOptions(other.GetOptions()).ToList();
        var options = EnumerateAllOptions(GetOptions()).ToList();

        if (otherOptions.Count != options.Count)
            return false;

        for (var i = 0; i < otherOptions.Count; i++)
        {
            var otherOption = otherOptions[i];
            var option = options[i];

            if (otherOption.Name != option.Name)
                return false;

            if (otherOption.Description != option.Description)
                return false;

            var otherChannelTypes = otherOption.GetChannelTypes().ToList();
            var channelTypes = option.GetChannelTypes().ToList();

            if (otherChannelTypes.Count != channelTypes.Count)
                return false;

            if (!otherChannelTypes.SequenceEqual(channelTypes))
                return false;
        }
        
        return true;
    }

    private static IEnumerable<LuaSlashCommandOption> EnumerateAllOptions(IEnumerable<LuaSlashCommandOption> options)
    {
        foreach (var option in options)
        {
            yield return option;

            foreach (var subOption in EnumerateAllOptions(option.GetOptions()))
            {
                yield return subOption;
            }
        }
    }
}