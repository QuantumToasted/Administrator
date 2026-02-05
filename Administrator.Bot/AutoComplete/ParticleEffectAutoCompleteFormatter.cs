using Administrator.Core;
using Backpack.Net;
using Humanizer;
using Qmmands;

namespace Administrator.Bot.AutoComplete;

public sealed class ParticleEffectAutoCompleteFormatter : IAutoCompleteFormatter<ParticleEffect, string>
{
    public static string FormatAutoCompleteName(ICommandContext context, ParticleEffect model) => model.Humanize(LetterCasing.Title).TrimStart('_');
    public static string FormatAutoCompleteValue(ICommandContext context, ParticleEffect model) => model.ToString("G");
    public static string[] FormatComparisonValues(ICommandContext context, ParticleEffect model) => [model.Humanize(LetterCasing.Title).TrimStart('_')];
}