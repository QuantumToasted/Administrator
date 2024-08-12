using System.Text;
using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Bot;
using Disqord.Bot.Commands;
using Disqord.Gateway;
using Qmmands;

namespace Administrator.Bot.AutoComplete;

public sealed class ButtonRoleAutoCompleteFormatter : IAutoCompleteFormatter<IDiscordCommandContext, ButtonRole, int>
{
    public string FormatAutoCompleteName(IDiscordCommandContext context, ButtonRole model)
    {
        return new StringBuilder($"{model.Id} - role ")
            .Append(context.Bot.GetRole(model.GuildId, model.RoleId) is { } role
                ? role.Name
                : model.RoleId.ToString())
            .Append(" - ")
            .Append(model.Text ?? "[emoji]")
            .ToString();
    }

    public int FormatAutoCompleteValue(IDiscordCommandContext context, ButtonRole model)
        => model.Id;

    public Func<IDiscordCommandContext, ButtonRole, string[]> ComparisonSelector => static (_, model) => [model.Text ?? model.Id.ToString(), model.Id.ToString()];
}