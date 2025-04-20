using System.Text;
using Administrator.Core;
using Administrator.Database;
using Disqord.Bot.Commands;
using Disqord.Gateway;

namespace Administrator.Bot.AutoComplete;

public sealed class ButtonRoleAutoCompleteFormatter : IAutoCompleteFormatter<IDiscordCommandContext, ButtonRole, int>
{
    public static string FormatAutoCompleteName(IDiscordCommandContext context, ButtonRole model)
    {
        return new StringBuilder($"{model.Id} - role ")
            .Append(context.Bot.GetRole(model.GuildId, model.RoleId) is { } role
                ? role.Name
                : model.RoleId.ToString())
            .Append(" - ")
            .Append(model.Text ?? "[emoji]")
            .ToString();
    }

    public static int FormatAutoCompleteValue(IDiscordCommandContext context, ButtonRole model) => model.Id;

    public static string[] FormatComparisonValues(IDiscordCommandContext context, ButtonRole model) => [model.Text ?? model.Id.ToString(), model.Id.ToString()];
}