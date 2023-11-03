using System.Text;
using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Bot;
using Disqord.Gateway;

namespace Administrator.Bot.AutoComplete;

public sealed class HighlightAutoCompleteFormatter : IAutoCompleteFormatter<Highlight, int>
{
    public string[] FormatAutoCompleteNames(IClient client, Highlight model)
    {
        var bot = (DiscordBotBase)client;
        var builder = new StringBuilder($"#{model.Id}")
            .Append($" - \"{model.Text}\" - ")
            .Append(model.GuildId.HasValue
                ? $"In {bot.GetGuild(model.GuildId.Value)?.Name ?? model.GuildId.Value.ToString()}"
                : "[GLOBAL]");

        return new[] { builder.ToString() };
    }

    public int FormatAutoCompleteValue(IClient client, Highlight model)
        => model.Id;

    public Func<Highlight, string[]> ComparisonSelector => static highlight => new[] { highlight.Text };
}