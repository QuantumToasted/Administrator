using Administrator.Core;
using Disqord.Bot.Commands.Application;

namespace Administrator.Bot.AutoComplete;

public sealed class SlashCommandAutoCompleteFormatter : IAutoCompleteFormatter<ApplicationCommand, string>
{
    public string FormatAutoCompleteName(ApplicationCommand model)
        => $"/{SlashCommandMentionService.GetPath(model)}";

    public string FormatAutoCompleteValue(ApplicationCommand model)
        => SlashCommandMentionService.GetPath(model)!;

    public Func<ApplicationCommand, string[]> ComparisonSelector => static model => [SlashCommandMentionService.GetPath(model)!];
}