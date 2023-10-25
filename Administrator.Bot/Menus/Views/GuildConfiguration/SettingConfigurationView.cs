using System.ComponentModel;
using System.Reflection;
using System.Text;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Extensions.Interactivity.Menus;

namespace Administrator.Bot;

public sealed class SettingConfigurationView : GuildConfigurationViewBase
{
    public const string SELECTION_TEXT = "Settings";

    private GuildSettings _settings;
    
    public SettingConfigurationView(IDiscordApplicationGuildCommandContext context, Guild guildConfig) 
        : base(context)
    {
        _settings = guildConfig.Settings;

        foreach (var flag in Enum.GetValues<GuildSettings>())
        {
            var flagSet = _settings.HasFlag(flag);
            var button = new ButtonViewComponent(e => ToggleSettingAsync(e, flag))
            {
                Label = $"{flag} - " + (flagSet ? "enabled" : "disabled"),
                Style = flagSet ? LocalButtonComponentStyle.Success : LocalButtonComponentStyle.Danger
            };
            
            AddComponent(button);
        }
    }
    
    protected override string FormatContent()
    {
        var builder = new StringBuilder(SELECTION_TEXT)
            .AppendNewline();
            
        foreach (var flag in Enum.GetValues<GuildSettings>())
        {
            var description = typeof(GuildSettings).GetField(flag.ToString())!.GetCustomAttribute<DescriptionAttribute>()!.Description;
            builder.AppendNewline($"{Markdown.Bold(flag)} - {description}");
        }

        return builder.ToString();
    }

    public async ValueTask ToggleSettingAsync(ButtonEventArgs e, GuildSettings flag)
    {
        var flagSet = _settings.HasFlag(flag);
        
        _settings = flagSet
            ? _settings & ~flag
            : _settings | flag;

        flagSet = _settings.HasFlag(flag);

        e.Button.Label = $"{flag} - " + (flagSet ? "enabled" : "disabled");
        e.Button.Style = flagSet ? LocalButtonComponentStyle.Success : LocalButtonComponentStyle.Danger;
        ReportChanges();

        await using var scope = _context.Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        var guildConfig = await db.GetOrCreateGuildConfigAsync(_context.GuildId);
        guildConfig.Settings = _settings;
        await db.SaveChangesAsync();
    }
}