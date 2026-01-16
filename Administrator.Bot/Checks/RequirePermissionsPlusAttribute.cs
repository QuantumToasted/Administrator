using Administrator.Core;
using Administrator.Database;
using Disqord.Bot.Commands;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;

namespace Administrator.Bot;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RequirePermissionsPlusAttribute(PermissionsPlus requiredPermissions) : DiscordGuildCheckAttribute
{
    public PermissionsPlus RequiredPermissions { get; } = requiredPermissions;
    
    public override async ValueTask<IResult> CheckAsync(IDiscordGuildCommandContext context)
    {
        await using var scope = context.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();

        var memberPermissions = await db.Permissions.GetPermissions(context.Author);
        var grantedPermissions = memberPermissions.Aggregate(PermissionsPlus.None, (p, perms) => p | perms.Permissions);

        if ((grantedPermissions & RequiredPermissions) != RequiredPermissions)
            return Results.Failure($"You lack the following extra permissions to execute this command: {RequiredPermissions:F}");

        return Results.Success;
    }
}