using Administrator.Core;
using Disqord.Bot.Commands.Application;
using Qmmands;

namespace Administrator.Bot.AutoComplete;

public sealed class SlashCommandAutoCompleteFormatter : IAutoCompleteFormatter<ApplicationCommand, string>
{
    public string FormatAutoCompleteName2(ApplicationCommand model)
        => $"/{SlashCommandMentionService.GetPath(model)}";

    public string FormatAutoCompleteValue2(ApplicationCommand model)
        => SlashCommandMentionService.GetPath(model)!;

    public Func<ApplicationCommand, string[]> ComparisonSelector => static model => [SlashCommandMentionService.GetPath(model)!];
    
    public static string FormatAutoCompleteName(ICommandContext context, ApplicationCommand model)
    {
        throw new NotImplementedException();
    }

    public static string FormatAutoCompleteValue(ICommandContext context, ApplicationCommand model)
    {
        throw new NotImplementedException();
    }

    public static string[] FormatComparisonValues(ICommandContext context, ApplicationCommand model)
    {
        throw new NotImplementedException();
    }
}