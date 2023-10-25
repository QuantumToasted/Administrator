using Disqord;
using Qmmands;
using Qommon;

namespace Administrator.Core;

public sealed class JsonEmbedAuthor
{
    public string Name { get; init; } = null!;

    public string? IconUrl { get; init; }

    public string? Url { get; init; }

    public async ValueTask<LocalEmbedAuthor> ToLocalAuthorAsync(IPlaceholderFormatter formatter, ICommandContext? context = null)
    {
        var author = new LocalEmbedAuthor()
            .WithName(await formatter.ReplacePlaceholdersAsync(Name, context));

        if (!string.IsNullOrWhiteSpace(IconUrl))
            author.WithIconUrl(IconUrl);

        if (!string.IsNullOrWhiteSpace(Url))
            author.WithUrl(Url);

        return author;
    }

    public static JsonEmbedAuthor FromEmbedAuthor(IEmbedAuthor author)
    {
        return new JsonEmbedAuthor
        {
            Name = author.Name,
            IconUrl = author.IconUrl,
            Url = author.Url
        };
    }

    public static JsonEmbedAuthor FromEmbedAuthor(LocalEmbedAuthor author)
    {
        return new JsonEmbedAuthor
        {
            Name = author.Name.Value,
            IconUrl = author.IconUrl.GetValueOrDefault(),
            Url = author.Url.GetValueOrDefault()
        };
    }
}