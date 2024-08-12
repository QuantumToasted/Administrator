using Disqord;
using Laylua;
using Qommon;

namespace Administrator.Bot;

public sealed record LuaSlashCommandOption : ILuaModel<LuaSlashCommandOption>
{
    public LuaSlashCommandOption(string name, LuaTable table)
    {
        Name = name;
        
        var type = table.GetValueOrDefault<string, string>("type") ?? "string";
        Type = Enum.Parse<SlashCommandOptionType>(type, true);

        if (Type is SlashCommandOptionType.SubcommandGroup or SlashCommandOptionType.Subcommand)
        {
            Options = table.GetValueOrDefault<string, LuaTable>("options");
        }

        if (Type is not SlashCommandOptionType.SubcommandGroup)
        {
            var description = table.GetValueOrDefault<string, string>("description") ?? "No description.";
            Guard.IsNotNullOrWhiteSpace(description);
            Guard.HasSizeLessThanOrEqualTo(description, Type is SlashCommandOptionType.Subcommand
                ? Discord.Limits.ApplicationCommand.MaxDescriptionLength
                : Discord.Limits.ApplicationCommand.Option.MaxDescriptionLength);

            Description = description;
        }

        if (Type is not SlashCommandOptionType.Subcommand && table.TryGetValue<string, bool>("required", out var required))
            IsRequired = required;

        if (Type is SlashCommandOptionType.Channel && table.TryGetValue<string, LuaTable>("channelTypes", out var channelTypes))
            ChannelTypes = channelTypes;

        if (Type is SlashCommandOptionType.Number or SlashCommandOptionType.Integer or SlashCommandOptionType.String)
        {
            Minimum = table.TryGetValue<string, double>("minimum", out var minimum) ? minimum : null;
            Maximum = table.TryGetValue<string, double>("maximum", out var maximum) ? maximum : null;
        }
    }
    
    public string Name { get; }
    
    public SlashCommandOptionType Type { get; }
    
    public string? Description { get; }

    public bool IsRequired { get; } = true;
    
    // TODO? public IReadOnlyList<ISlashCommandOptionChoice> Choices { get; }
    
    public LuaTable? Options { get; }
    
    public LuaTable? ChannelTypes { get; }
    
    public double? Minimum { get; }
    
    public double? Maximum { get; }

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

    public IEnumerable<ChannelType> GetChannelTypes()
    {
        if (ChannelTypes is null)
            yield break;
        
        foreach (var channelTypePair in ChannelTypes)
        {
            Guard.IsNotNull(channelTypePair.Value.Value);
            var channelType = Enum.Parse<ChannelType>(Guard.IsOfType<string>(channelTypePair.Value.Value), true);
            yield return channelType;
        }
    }
}