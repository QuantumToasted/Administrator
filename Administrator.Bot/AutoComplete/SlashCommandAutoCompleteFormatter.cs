using Administrator.Core;
using Disqord.Bot.Commands.Application;
using Qmmands;

namespace Administrator.Bot.AutoComplete;

public sealed class SlashCommandAutoCompleteFormatter : IAutoCompleteFormatter<ApplicationCommand, string>
{
    public static string FormatAutoCompleteName(ICommandContext context, ApplicationCommand model) => $"/{SlashCommandMentionService.GetPath(model)}";

    public static string FormatAutoCompleteValue(ICommandContext context, ApplicationCommand model) => SlashCommandMentionService.GetPath(model)!;

    public static string[] FormatComparisonValues(ICommandContext context, ApplicationCommand model) => [SlashCommandMentionService.GetPath(model)!];
}