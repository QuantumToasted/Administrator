using Disqord;
using Disqord.Bot.Commands;
using Qmmands;

namespace Administrator.Bot;

public static class CommandServiceExtensions
{
    public static Permissions GetRequiredBotPermissions(this ICommandService commands)
    {
        var modules = commands.EnumerateModules().ToList();

        var modulePermissions = modules.SelectMany(x => x.Value)
            .SelectMany(x => x.Checks)
            .OfType<RequireBotPermissionsAttribute>()
            .Aggregate(Permissions.None, (p, attr) => p | attr.Permissions);
        
        var requiredBotPermissions = modules.SelectMany(x => x.Value)
            .SelectMany(x => x.Commands)
            .SelectMany(x => x.Checks)
            .OfType<RequireBotPermissionsAttribute>()
            .Aggregate(Permissions.None, (p, attr) => p | attr.Permissions);
        
        return modulePermissions | requiredBotPermissions;
    }
}