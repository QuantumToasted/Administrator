using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Disqord;
using Qmmands;
using Qommon;

namespace Administrator.Core;

public sealed class JsonMessage
{
    public string? Content { get; init; }

    public List<JsonEmbed>? Embeds { get; init; }

    public async ValueTask<TMessage> ToLocalMessageAsync<TMessage>(IPlaceholderFormatter formatter, ICommandContext? context = null)
        where TMessage : LocalMessageBase, new()
    {
        var message = new TMessage();

        if (!string.IsNullOrWhiteSpace(Content))
            message.WithContent(await formatter.ReplacePlaceholdersAsync(Content, context));

        if (Embeds?.Count > 0)
        {
            var embeds = new List<LocalEmbed>();
            foreach (var embed in Embeds)
            {
                embeds.Add(await embed.ToLocalEmbedAsync(formatter, context));
            }

            message.WithEmbeds(embeds);
        }

        return message;
    }

    public static JsonMessage FromMessage(IUserMessage message)
    {
        return new JsonMessage
        {
            Content = string.IsNullOrWhiteSpace(message.Content) ? null : message.Content,
            Embeds = message.Embeds.Count > 0 ? message.Embeds.Select(JsonEmbed.FromEmbed).ToList() : null
        };
    }

    public static JsonMessage FromMessage<TMessage>(TMessage message)
        where TMessage : LocalMessageBase
    {
        return new JsonMessage
        {
            Content = message.Content.GetValueOrDefault(),
            Embeds = message.Embeds.GetValueOrDefault()?.Select(JsonEmbed.FromEmbed).ToList()
        };
    }

    public static bool TryParse(string str, [NotNullWhen(true)] out JsonMessage? message, [NotNullWhen(false)] out string? error)
    {
        message = null;
        error = null;

        try
        {
            message = Parse(str);
            return true;
        }
        catch (JsonException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static JsonMessage Parse(string str)
        => JsonSerializer.Deserialize<JsonMessage>(str)!;
}