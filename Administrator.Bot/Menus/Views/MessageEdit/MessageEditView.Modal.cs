using Disqord;
using Qommon;
using Modal = Disqord.LocalInteractionModalResponse;

namespace Administrator.Bot;

public abstract partial class MessageEditView
{
    private Modal ModifyContentModal()
    {
        var contentInput = LocalComponent.TextInput("content", "Content", TextInputComponentStyle.Paragraph)
            .WithMaximumInputLength(Discord.Limits.Message.MaxContentLength)
            .WithIsRequired(false)
            .WithPlaceholder("Leave blank to remove");

        if (!string.IsNullOrWhiteSpace(Message.Content.GetValueOrDefault()))
            contentInput.WithPrefilledValue(Message.Content.Value!);

        return new Modal()
            .WithTitle("Modify Content")
            .WithCustomId($"Message:Modify:Content:{Menu.MessageId}")
            .AddComponent(LocalComponent.Row(contentInput));
    }

    private Modal ModifyEmbedColorModal()
    {
        var colorInput = LocalComponent.TextInput("color", "Color", TextInputComponentStyle.Short)
            .WithMaximumInputLength(20)
            .WithIsRequired(false)
            .WithPlaceholder("Leave blank to remove");

        var embed = Message.Embeds.Value[_embedIndex];
        if (embed.Color.HasValue)
            colorInput.WithPrefilledValue(embed.Color.Value.ToString());

        return new Modal()
            .WithTitle("Modify Color")
            .WithCustomId($"Embed:Modify:Color:{_embedIndex}:{Menu.MessageId}")
            .AddComponent(LocalComponent.Row(colorInput));
    }

    private Modal ModifyEmbedAuthorModal()
    {
        var nameInput = LocalComponent.TextInput("name", "Name", TextInputComponentStyle.Paragraph)
            .WithMaximumInputLength(Discord.Limits.Message.Embed.Author.MaxNameLength)
            .WithIsRequired(false)
            .WithPlaceholder("Leave blank to remove");

        var iconUrlInput = LocalComponent.TextInput("iconUrl", "Icon URL", TextInputComponentStyle.Short)
            .WithIsRequired(false)
            .WithPlaceholder("Leave blank to remove");

        var urlInput = LocalComponent.TextInput("url", "URL", TextInputComponentStyle.Short)
            .WithIsRequired(false)
            .WithPlaceholder("Leave blank to remove");

        var embed = Message.Embeds.Value[_embedIndex];
        if (embed.Author.GetValueOrDefault() is { } author)
        {
            if (author.Name.HasValue)
                nameInput.WithPrefilledValue(author.Name.Value);

            if (author.IconUrl.HasValue)
                iconUrlInput.WithPrefilledValue(author.IconUrl.Value);

            if (author.Url.HasValue)
                iconUrlInput.WithPrefilledValue(author.Url.Value);
        }

        return new Modal()
            .WithTitle("Modify Author")
            .WithCustomId($"Embed:Modify:Author:{_embedIndex}:{Menu.MessageId}")
            .WithComponents(LocalComponent.Row(nameInput), LocalComponent.Row(iconUrlInput), LocalComponent.Row(urlInput));
    }

    private Modal ModifyEmbedTitleModal()
    {
        var titleInput = LocalComponent.TextInput("title", "Title", TextInputComponentStyle.Paragraph)
            .WithMaximumInputLength(Discord.Limits.Message.Embed.MaxTitleLength)
            .WithIsRequired(false)
            .WithPlaceholder("Leave blank to remove");

        var embed = Message.Embeds.Value[_embedIndex];
        if (embed.Title.HasValue)
            titleInput.WithPrefilledValue(embed.Title.Value);

        return new Modal()
            .WithTitle("Modify Title")
            .WithCustomId($"Embed:Modify:Title:{_embedIndex}:{Menu.MessageId}")
            .AddComponent(LocalComponent.Row(titleInput));
    }

    private Modal ModifyEmbedDescriptionModal()
    {
        var descriptionInput = LocalComponent.TextInput("description", "Description", TextInputComponentStyle.Paragraph)
            .WithMaximumInputLength(4000)
            .WithIsRequired(false)
            .WithPlaceholder("Leave blank to remove");

        var embed = Message.Embeds.Value[_embedIndex];
        if (embed.Description.HasValue)
            descriptionInput.WithPrefilledValue(embed.Description.Value);

        return new Modal()
            .WithTitle("Modify Description")
            .WithCustomId($"Embed:Modify:Description:{_embedIndex}:{Menu.MessageId}")
            .AddComponent(LocalComponent.Row(descriptionInput));
    }

    private Modal ModifyEmbedImageUrlModal()
    {
        var imageUrlInput = LocalComponent.TextInput("imageUrl", "Image URL", TextInputComponentStyle.Short)
            .WithIsRequired(false)
            .WithPlaceholder("Leave blank to remove");

        var embed = Message.Embeds.Value[_embedIndex];
        if (embed.ImageUrl.HasValue)
            imageUrlInput.WithPrefilledValue(embed.ImageUrl.Value);

        return new Modal()
            .WithTitle("Modify Image URL")
            .WithCustomId($"Embed:Modify:ImageUrl:{_embedIndex}:{Menu.MessageId}")
            .AddComponent(LocalComponent.Row(imageUrlInput));
    }

    private Modal ModifyEmbedThumbnailUrlModal()
    {
        var thumbnailUrl = LocalComponent.TextInput("thumbnailUrl", "Thumbnail Image URL", TextInputComponentStyle.Short)
            .WithIsRequired(false)
            .WithPlaceholder("Leave blank to remove");

        var embed = Message.Embeds.Value[_embedIndex];
        if (embed.ThumbnailUrl.HasValue)
            thumbnailUrl.WithPrefilledValue(embed.ThumbnailUrl.Value);

        return new Modal()
            .WithTitle("Modify Thumbnail Image URL")
            .WithCustomId($"Embed:Modify:ThumbnailUrl:{_embedIndex}:{Menu.MessageId}")
            .AddComponent(LocalComponent.Row(thumbnailUrl));
    }

    private Modal ModifyEmbedFooterModal()
    {
        var textInput = LocalComponent.TextInput("text", "Text", TextInputComponentStyle.Paragraph)
            .WithMaximumInputLength(Discord.Limits.Message.Embed.Footer.MaxTextLength)
            .WithIsRequired(false)
            .WithPlaceholder("Leave blank to remove");

        var iconUrlInput = LocalComponent.TextInput("iconUrl", "Icon URL", TextInputComponentStyle.Short)
            .WithIsRequired(false)
            .WithPlaceholder("Leave blank to remove");

        var embed = Message.Embeds.Value[_embedIndex];
        if (embed.Footer.GetValueOrDefault() is { } footer)
        {
            if (footer.Text.HasValue)
                textInput.WithPrefilledValue(footer.Text.Value);

            if (footer.IconUrl.HasValue)
                iconUrlInput.WithPrefilledValue(footer.IconUrl.Value);
        }

        return new Modal()
            .WithTitle("Modify Footer")
            .WithCustomId($"Embed:Modify:Footer:{_embedIndex}:{Menu.MessageId}")
            .WithComponents(LocalComponent.Row(textInput), LocalComponent.Row(iconUrlInput));
    }

    private Modal ModifyEmbedFieldModal()
    {
        var nameInput = LocalComponent.TextInput("name", "Name", TextInputComponentStyle.Paragraph)
            .WithMaximumInputLength(Discord.Limits.Message.Embed.Field.MaxNameLength)
            .WithIsRequired(false)
            .WithPlaceholder("Leave blank to remove");

        var valueInput = LocalComponent.TextInput("value", "Value", TextInputComponentStyle.Paragraph)
            .WithMaximumInputLength(Discord.Limits.Message.Embed.Field.MaxValueLength)
            .WithIsRequired(false)
            .WithPlaceholder("Leave blank for a blank value");

        var inlineInput = LocalComponent.TextInput("inline", "Inline?", TextInputComponentStyle.Short)
            .WithMaximumInputLength(5)
            .WithIsRequired()
            .WithPlaceholder("True/False");

        var embed = Message.Embeds.Value[_embedIndex];
        var field = embed.Fields.Value[_fieldIndex];

        if (field.Name.HasValue)
            nameInput.WithPrefilledValue(field.Name.Value);

        if (field.Value.HasValue)
            valueInput.WithPrefilledValue(field.Value.Value);

        inlineInput.WithPrefilledValue(field.IsInline.GetValueOrDefault().ToString());

        return new Modal()
            .WithTitle($"Modify Field {_fieldIndex}")
            .WithCustomId($"Embed:Modify:Field:{_embedIndex}:{_fieldIndex}:{Menu.MessageId}")
            .WithComponents(LocalComponent.Row(nameInput), LocalComponent.Row(valueInput), LocalComponent.Row(inlineInput));
    }

    // TODO: Timestamp
}