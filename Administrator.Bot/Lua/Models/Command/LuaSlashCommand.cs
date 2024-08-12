using System.Text;
using Disqord;
using Humanizer;
using Laylua;
using Qommon;

namespace Administrator.Bot;

public sealed class LuaSlashCommand : ILuaModel<LuaSlashCommand>
{
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
}