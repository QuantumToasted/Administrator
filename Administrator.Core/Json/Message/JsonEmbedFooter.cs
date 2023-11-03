using Disqord;
using Qmmands;
using Qommon;

namespace Administrator.Core;

public sealed class JsonEmbedFooter
{
    public string Text { get; init; } = null!;

    public string? IconUrl { get; init; }

    public async ValueTask<LocalEmbedFooter> ToLocalFooterAsync(IPlaceholderFormatter formatter, ICommandContext? context = null)
    {
        var footer = new LocalEmbedFooter()
            .WithText(await formatter.ReplacePlaceholdersAsync(Text, context));

        if (!string.IsNullOrWhiteSpace(IconUrl))
            footer.WithIconUrl(IconUrl);

        return footer;
    }

    public static JsonEmbedFooter FromEmbedFooter(IEmbedFooter footer)
    {
        return new JsonEmbedFooter
        {
            Text = footer.Text,
            IconUrl = footer.IconUrl
        };
    }

    public static JsonEmbedFooter FromEmbedFooter(LocalEmbedFooter footer)
    {
        return new JsonEmbedFooter
        {
            Text = footer.Text.Value,
            IconUrl = footer.IconUrl.GetValueOrDefault()
        };
    }
}