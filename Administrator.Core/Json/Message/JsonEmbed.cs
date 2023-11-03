using Disqord;
using System.Text.Json.Serialization;
using Qmmands;
using Qommon;

namespace Administrator.Core;

public sealed class JsonEmbed
{
    [JsonConverter(typeof(ColorJsonConverter))]
    public Color? Color { get; init; }

    public JsonEmbedAuthor? Author { get; init; }

    public string? Url { get; init; }

    public string? Title { get; init; }

    public string? Description { get; init; }

    public List<JsonEmbedField>? Fields { get; init; }

    public string? ImageUrl { get; init; }

    public string? ThumbnailUrl { get; init; }

    public JsonEmbedFooter? Footer { get; init; }

    // Not using Instant here because it's not worth installing an entire package just for serializing it
    public DateTimeOffset? Timestamp { get; init; }

    public async ValueTask<LocalEmbed> ToLocalEmbedAsync(IPlaceholderFormatter formatter, ICommandContext? context = null)
    {
        var embed = new LocalEmbed();

        if (Color.HasValue)
            embed.WithColor(Color.Value);

        if (Author is not null)
            embed.WithAuthor(await Author.ToLocalAuthorAsync(formatter, context));

        if (!string.IsNullOrWhiteSpace(Url))
            embed.WithUrl(Url);

        if (!string.IsNullOrWhiteSpace(Title))
            embed.WithTitle(await formatter.ReplacePlaceholdersAsync(Title, context));

        if (!string.IsNullOrWhiteSpace(Description))
            embed.WithDescription(await formatter.ReplacePlaceholdersAsync(Description, context));

        if (Fields?.Count > 0)
        {
            // embed.WithFields(Fields.Select(x => x.ToLocalField()));
            var fields = new List<LocalEmbedField>();
            foreach (var field in Fields)
            {
                fields.Add(await field.ToLocalFieldAsync(formatter, context));
            }

            embed.WithFields(fields);
        }

        if (!string.IsNullOrWhiteSpace(ImageUrl))
            embed.WithImageUrl(ImageUrl);

        if (!string.IsNullOrWhiteSpace(ThumbnailUrl))
            embed.WithThumbnailUrl(ThumbnailUrl);

        if (Footer is not null)
            embed.WithFooter(await Footer.ToLocalFooterAsync(formatter, context));

        if (Timestamp.HasValue)
            embed.WithTimestamp(Timestamp.Value);

        return embed;
    }

    public static JsonEmbed FromEmbed(IEmbed embed)
    {
        return new JsonEmbed
        {
            Color = embed.Color,
            Author = embed.Author is { } author ? JsonEmbedAuthor.FromEmbedAuthor(author) : null,
            Url = embed.Url,
            Title = embed.Title,
            Description = embed.Description,
            Fields = embed.Fields.Count > 0 ? embed.Fields.Select(JsonEmbedField.FromEmbedField).ToList() : null,
            ImageUrl = embed.Image?.Url,
            ThumbnailUrl = embed.Thumbnail?.Url,
            Footer = embed.Footer is { } footer ? JsonEmbedFooter.FromEmbedFooter(footer) : null,
            Timestamp = embed.Timestamp
        };
    }

    public static JsonEmbed FromEmbed(LocalEmbed embed)
    {
        return new JsonEmbed
        {
            Color = embed.Color.GetValueOrNullable(),
            Author = embed.Author.GetValueOrDefault() is { } author ? JsonEmbedAuthor.FromEmbedAuthor(author) : null,
            Url = embed.Url.GetValueOrDefault(),
            Title = embed.Title.GetValueOrDefault(),
            Description = embed.Description.GetValueOrDefault(),
            Fields = embed.Fields.GetValueOrDefault()?.Select(JsonEmbedField.FromEmbedField).ToList(),
            ImageUrl = embed.ImageUrl.GetValueOrDefault(),
            ThumbnailUrl = embed.ThumbnailUrl.GetValueOrDefault(),
            Footer = embed.Footer.GetValueOrDefault() is { } footer ? JsonEmbedFooter.FromEmbedFooter(footer) : null,
            Timestamp = embed.Timestamp.GetValueOrNullable()
        };
    }
}