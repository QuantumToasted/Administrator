using Disqord;
using Disqord.Bot.Commands.Components;
using Qommon;
using IResult = Qmmands.IResult;

namespace Administrator.Bot;

public sealed class MessageEditComponentModule : DiscordComponentGuildModuleBase
{
    private readonly MessageEditViewService _viewService;

    public MessageEditComponentModule(MessageEditViewService viewService)
    {
        _viewService = viewService;
    }

    [ModalCommand("Message:Modify:Content:*")]
    public async Task<IResult> ModifyContentAsync(Snowflake messageId, string? content = null)
    {
        if (!_viewService.Views.TryGetValue(messageId, out var view))
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

    [ModalCommand("Embed:Modify:Color:*:*")]
    public async Task<IResult> ModifyEmbedColorAsync(int embedIndex, Snowflake messageId, Color? color = null)
    {
        if (!_viewService.Views.TryGetValue(messageId, out var view))
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
        return Response($"Embed {embedIndex} color updated.").AsEphemeral();
    }

    [ModalCommand("Embed:Modify:Author:*:*")]
    public async Task<IResult> ModifyEmbedAuthorAsync(int embedIndex, Snowflake messageId, string? name = null,
        string? iconUrl = null, string? url = null)
    {
        if (!_viewService.Views.TryGetValue(messageId, out var view))
            return Response("Invalid message ID. You may have to re-run this command again.").AsEphemeral();

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
        return Response($"Embed {embedIndex} author updated.").AsEphemeral();
    }

    [ModalCommand("Embed:Modify:Title:*:*")]
    public async Task<IResult> ModifyEmbedTitleAsync(int embedIndex, Snowflake messageId, string? title = null)
    {
        if (!_viewService.Views.TryGetValue(messageId, out var view))
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
        return Response($"Embed {embedIndex} title updated.").AsEphemeral();
    }

    [ModalCommand("Embed:Modify:Description:*:*")]
    public async Task<IResult> ModifyEmbedDescriptionAsync(int embedIndex, Snowflake messageId, string? description = null)
    {
        if (!_viewService.Views.TryGetValue(messageId, out var view))
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
        return Response($"Embed {embedIndex} description updated.").AsEphemeral();
    }

    [ModalCommand("Embed:Modify:ImageUrl:*:*")]
    public async Task<IResult> ModifyEmbedImageUrlAsync(int embedIndex, Snowflake messageId, string? imageUrl = null)
    {
        if (!_viewService.Views.TryGetValue(messageId, out var view))
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
        return Response($"Embed {embedIndex} image URL updated.").AsEphemeral();
    }

    [ModalCommand("Embed:Modify:ThumbnailUrl:*:*")]
    public async Task<IResult> ModifyEmbedThumbnailUrlAsync(int embedIndex, Snowflake messageId, string? thumbnailUrl = null)
    {
        if (!_viewService.Views.TryGetValue(messageId, out var view))
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
        return Response($"Embed {embedIndex} thumbnail image URL updated.").AsEphemeral();
    }

    [ModalCommand("Embed:Modify:Footer:*:*")]
    public async Task<IResult> ModifyEmbedFooterAsync(int embedIndex, Snowflake messageId, string? text = null,
        string? iconUrl = null)
    {
        if (!_viewService.Views.TryGetValue(messageId, out var view))
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
        return Response($"Embed {embedIndex} footer updated.").AsEphemeral();
    }

    [ModalCommand("Embed:Modify:Field:*:*:*")]
    public async Task<IResult> ModifyEmbedFieldAsync(int embedIndex, int fieldIndex, Snowflake messageId,
        string? name = null, string? value = null, bool? inline = null)
    {
        if (!_viewService.Views.TryGetValue(messageId, out var view))
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
        return Response($"Embed {embedIndex} field {fieldIndex} updated.").AsEphemeral();
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