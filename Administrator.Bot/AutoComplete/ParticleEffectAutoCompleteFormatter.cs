using Administrator.Core;
using Backpack.Net;
using Humanizer;

namespace Administrator.Bot.AutoComplete;

public sealed class ParticleEffectAutoCompleteFormatter : IAutoCompleteFormatter<ParticleEffect, string>
{
    public string FormatAutoCompleteName(ParticleEffect model)
        => model.Humanize(LetterCasing.Title).TrimStart('_');

    public string FormatAutoCompleteValue(ParticleEffect model)
        => model.ToString("G");

    public Func<ParticleEffect, string[]> ComparisonSelector => static model => [model.Humanize(LetterCasing.Title).TrimStart('_')];
}