using System.Text;
using Administrator.Core;
using Administrator.Database;
using Disqord.Bot.Commands;
using Disqord.Gateway;

namespace Administrator.Bot.AutoComplete;

public sealed class HighlightAutoCompleteFormatter : IAutoCompleteFormatter<IDiscordCommandContext, Highlight, int>
{
    public string FormatAutoCompleteName(IDiscordCommandContext context, Highlight model)
    {
        var builder = new StringBuilder($"#{model.Id}")
            .Append($" - \"{model.Text}\" - ")
            .Append(model.GuildId.HasValue
                ? $"In {context.Bot.GetGuild(model.GuildId.Value)?.Name ?? model.GuildId.Value.ToString()}"
                : "[GLOBAL]");

        return builder.ToString();
    }

    public int FormatAutoCompleteValue(IDiscordCommandContext context, Highlight model)
        => model.Id;

    public Func<IDiscordCommandContext, Highlight, string[]> ComparisonSelector => static (_, model) => [model.Text];
}