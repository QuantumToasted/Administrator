using System.Text;
using Administrator.Database;
using Disqord;
using Disqord.Bot;
using Disqord.Bot.Commands.Components;
using Disqord.Bot.Commands.Interaction;
using Disqord.Gateway;
using Disqord.Rest;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Qommon;

namespace Administrator.Bot;

public static partial class DbModelExtensions
{
    public static string GetCustomId(this ButtonRole buttonRole)
        => $"BR_{buttonRole.Id}";
    
    public static LocalButtonComponent ToButton(this ButtonRole buttonRole, DiscordBotBase bot)
    {
        var button = new LocalButtonComponent()
            .WithCustomId(buttonRole.GetCustomId())
            .WithStyle(buttonRole.Style);

        if (!string.IsNullOrWhiteSpace(buttonRole.Text))
            button.WithLabel(buttonRole.Text);

        if (buttonRole.Emoji is not null)
        {
            var emojiService = bot.Services.GetRequiredService<EmojiService>();
            button.WithEmoji(LocalEmoji.FromEmoji(emojiService.ParseEmoji(buttonRole.Emoji))!);
        }

        return button;
    }

    public static async ValueTask<IResult?> ExecuteAsync(this ButtonRole buttonRole, ICommandContext context)
    {
        var componentContext = Guard.IsAssignableToType<IDiscordComponentGuildCommandContext>(context);

        var buttonRoleName = componentContext.Bot.GetRole(buttonRole.GuildId, buttonRole.RoleId) is { } role
            ? Markdown.Bold(role.Name)
            : $"with ID {Markdown.Code(buttonRole.Id)}";

        if (componentContext.Author.RoleIds.Contains(buttonRole.RoleId))
        {
            try
            {
                await componentContext.Author.RevokeRoleAsync(buttonRole.RoleId);
                return new DiscordInteractionResponseCommandResult(componentContext, new LocalInteractionMessageResponse()
                    .WithContent($"You've removed the role {buttonRoleName} from yourself.")
                    .WithIsEphemeral());
            }
            catch (Exception ex)
            {
                return Results.Exception("ButtonRoleAssign", ex);
            }
        }

        var removedRoleIds = new List<Snowflake>();
        if (buttonRole.ExclusiveGroupId.HasValue)
        {
            await using var scope = context.Services.CreateAsyncScopeWithDatabase(out var db);
            var exclusiveGroupRoles = await db.ButtonRoles
                .Where(x => x.Id != buttonRole.Id && x.GuildId == componentContext.GuildId && x.ExclusiveGroupId == buttonRole.ExclusiveGroupId)
                .ToListAsync();

            removedRoleIds = exclusiveGroupRoles.Select(x => x.RoleId).Except([buttonRole.RoleId])
                .Where(x => componentContext.Author.RoleIds.Contains(x)).ToList();
        }

        var newRoleIds = componentContext.Author.RoleIds.Append(buttonRole.RoleId).Except(removedRoleIds).ToHashSet();
        
        var responseBuilder = new StringBuilder($"You've given yourself the role {buttonRoleName}!");

        if (removedRoleIds.Count > 0)
        {
            responseBuilder.AppendNewline()
                .AppendNewline("You've also had the following mutually exclusive role(s) removed:");

            foreach (var roleId in removedRoleIds)
            {
                var roleName = componentContext.Bot.GetRole(buttonRole.GuildId, roleId) is { } r
                    ? Markdown.Bold(r.Name)
                    : $"{Markdown.Code(buttonRole.Id)}";

                responseBuilder.AppendNewline(roleName);
            }
        }
        
        try
        {
            await componentContext.Author.ModifyAsync(x => x.RoleIds = newRoleIds);
            return new DiscordInteractionResponseCommandResult(componentContext, new LocalInteractionMessageResponse()
                .WithContent(responseBuilder.ToString())
                .WithIsEphemeral());
        }
        catch (Exception ex)
        {
            return Results.Exception("ButtonRoleAssign", ex);
        }
    }
}