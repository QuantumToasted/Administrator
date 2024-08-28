using Disqord;
using Disqord.Bot.Commands.Components;
using Qommon;
using IResult = Qmmands.IResult;

namespace Administrator.Bot;

public sealed partial class MessageEditComponentModule(MessageEditViewService viewService) : DiscordComponentGuildModuleBase
{
    public partial async Task<IResult> ModifyContent(Snowflake messageId, string? content)
    {
        if (!viewService.Views.TryGetValue(messageId, out var view))
            return Response("Invalid message ID. You may have to re-run this command again.").AsEphemeral();

        if (!string.IsNullOrWhiteSpace(content))
        {
            view.Message.WithContent(content);
        }
        else
        {
            view.Message.Content = null;
        }

        view.RegenerateComponents();
        await view.Menu.ApplyChangesAsync(view.Menu.LastEventArgs);

        return Response("Content updated.").AsEphemeral();
    }

    public partial async Task<IResult> ModifyEmbedColor(int embedIndex, Snowflake messageId, Color? color)
    {
        if (!viewService.Views.TryGetValue(messageId, out var view))
            return Response("Invalid message ID. You may have to re-run this command again.").AsEphemeral();

        var embeds = view.Message.Embeds.Value;
        if (color.HasValue)
        {
            embeds[embedIndex].WithColor(color.Value);
        }
        else
        {
            embeds[embedIndex].Color = Optional<Color>.Empty;
        }

        view.RegenerateComponents();
        await view.Menu.ApplyChangesAsync(view.Menu.LastEventArgs);
        return Response($"Embed {embedIndex + 1} color updated.").AsEphemeral();
    }

    public partial async Task<IResult> ModifyEmbedAuthor(int embedIndex, Snowflake messageId, string? name, string? iconUrl, string? url)
    {
        if (!viewService.Views.TryGetValue(messageId, out var view))
            return Response("Invalid message ID. You may have to re-run this command again.").AsEphemeral();

        if (string.IsNullOrWhiteSpace(name))
            return Response("The author name is required.").AsEphemeral();

        if (!IsNullOrWhiteSpaceOrValidUrl(iconUrl, true))
            return Response("Invalid author icon URL.").AsEphemeral();

        if (!IsNullOrWhiteSpaceOrValidUrl(url, false))
            return Response("Invalid author URL.").AsEphemeral();

        var embeds = view.Message.Embeds.Value;
        if (!string.IsNullOrWhiteSpace(name))
        {
            embeds[embedIndex].WithAuthor(name, iconUrl, url);
        }
        else
        {
            embeds[embedIndex].Author = Optional<LocalEmbedAuthor>.Empty;
        }

        view.RegenerateComponents();
        await view.Menu.ApplyChangesAsync(view.Menu.LastEventArgs);
        return Response($"Embed {embedIndex + 1} author updated.").AsEphemeral();
    }

    public partial async Task<IResult> ModifyEmbedTitle(int embedIndex, Snowflake messageId, string? title)
    {
        if (!viewService.Views.TryGetValue(messageId, out var view))
            return Response("Invalid message ID. You may have to re-run this command again.").AsEphemeral();

        var embeds = view.Message.Embeds.Value;
        if (!string.IsNullOrWhiteSpace(title))
        {
            embeds[embedIndex].WithTitle(title);
        }
        else
        {
            embeds[embedIndex].Title = Optional<string>.Empty;
        }

        view.RegenerateComponents();
        await view.Menu.ApplyChangesAsync(view.Menu.LastEventArgs);
        return Response($"Embed {embedIndex + 1} title updated.").AsEphemeral();
    }

    public partial async Task<IResult> ModifyEmbedDescription(int embedIndex, Snowflake messageId, string? description)
    {
        if (!viewService.Views.TryGetValue(messageId, out var view))
            return Response("Invalid message ID. You may have to re-run this command again.").AsEphemeral();

        var embeds = view.Message.Embeds.Value;
        if (!string.IsNullOrWhiteSpace(description))
        {
            embeds[embedIndex].WithDescription(description);
        }
        else
        {
            embeds[embedIndex].Description = Optional<string>.Empty;
        }

        view.RegenerateComponents();
        await view.Menu.ApplyChangesAsync(view.Menu.LastEventArgs);
        return Response($"Embed {embedIndex + 1} description updated.").AsEphemeral();
    }

    public partial async Task<IResult> ModifyEmbedImageUrl(int embedIndex, Snowflake messageId, string? imageUrl)
    {
        if (!viewService.Views.TryGetValue(messageId, out var view))
            return Response("Invalid message ID. You may have to re-run this command again.").AsEphemeral();
        
        if (!IsNullOrWhiteSpaceOrValidUrl(imageUrl, true))
            return Response("Invalid embed image URL.").AsEphemeral();

        var embeds = view.Message.Embeds.Value;
        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            embeds[embedIndex].WithImageUrl(imageUrl);
        }
        else
        {
            embeds[embedIndex].ImageUrl = Optional<string>.Empty;
        }

        view.RegenerateComponents();
        await view.Menu.ApplyChangesAsync(view.Menu.LastEventArgs);
        return Response($"Embed {embedIndex + 1} image URL updated.").AsEphemeral();
    }

    public partial async Task<IResult> ModifyEmbedThumbnailUrl(int embedIndex, Snowflake messageId, string? thumbnailUrl)
    {
        if (!viewService.Views.TryGetValue(messageId, out var view))
            return Response("Invalid message ID. You may have to re-run this command again.").AsEphemeral();
        
        if (!IsNullOrWhiteSpaceOrValidUrl(thumbnailUrl, true))
            return Response("Invalid embed thumbnail image URL.").AsEphemeral();

        var embeds = view.Message.Embeds.Value;
        if (!string.IsNullOrWhiteSpace(thumbnailUrl))
        {
            embeds[embedIndex].WithThumbnailUrl(thumbnailUrl);
        }
        else
        {
            embeds[embedIndex].ThumbnailUrl = Optional<string>.Empty;
        }

        view.RegenerateComponents();
        await view.Menu.ApplyChangesAsync(view.Menu.LastEventArgs);
        return Response($"Embed {embedIndex + 1} thumbnail image URL updated.").AsEphemeral();
    }

    public partial async Task<IResult> ModifyEmbedFooter(int embedIndex, Snowflake messageId, string? text, string? iconUrl)
    {
        if (!viewService.Views.TryGetValue(messageId, out var view))
            return Response("Invalid message ID. You may have to re-run this command again.").AsEphemeral();

        if (!IsNullOrWhiteSpaceOrValidUrl(iconUrl, true))
            return Response("Invalid footer icon image URL.").AsEphemeral();

        var embeds = view.Message.Embeds.Value;
        if (!string.IsNullOrWhiteSpace(text))
        {
            embeds[embedIndex].WithFooter(text, iconUrl);
        }
        else
        {
            embeds[embedIndex].Footer = Optional<LocalEmbedFooter>.Empty;
        }

        view.RegenerateComponents();
        await view.Menu.ApplyChangesAsync(view.Menu.LastEventArgs);
        return Response($"Embed {embedIndex + 1} footer updated.").AsEphemeral();
    }

    public partial async Task<IResult> ModifyEmbedField(int embedIndex, int fieldIndex, Snowflake messageId, string? name, string? value, bool? inline)
    {
        if (!viewService.Views.TryGetValue(messageId, out var view))
            return Response("Invalid message ID. You may have to re-run this command again.").AsEphemeral();

        var embeds = view.Message.Embeds.Value;
        if (!string.IsNullOrWhiteSpace(name))
        {
            var field = new LocalEmbedField()
                .WithName(name);

            if (!string.IsNullOrWhiteSpace(value))
            {
                field.WithValue(value);
            }
            else
            {
                field.WithBlankValue();
            }

            field.WithIsInline(inline.GetValueOrDefault());
            embeds[embedIndex].Fields.Value[fieldIndex] = field;
        }
        else
        {
            embeds[embedIndex].Fields.Value.RemoveAt(fieldIndex);
        }

        view.RegenerateComponents();
        await view.Menu.ApplyChangesAsync(view.Menu.LastEventArgs);
        return Response($"Embed {embedIndex + 1} field {fieldIndex + 1} updated.").AsEphemeral();
    }
    
    private static bool IsNullOrWhiteSpaceOrValidUrl(string? str, bool isAttachment)
    {
        if (string.IsNullOrWhiteSpace(str))
            return true;

        if (!Uri.TryCreate(str, UriKind.Absolute, out var uri))
            return false;

        return isAttachment
            ? uri.Scheme is "http" or "https" or "attachment"
            : uri.Scheme is "http" or "https";
    }
}