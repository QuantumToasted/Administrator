using Disqord;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Rest;
using Qommon;

namespace Administrator.Bot;

public abstract partial class MessageEditView : ViewBase
{
    private bool _firstFormat = true;
    private int _embedIndex;
    private int _fieldIndex;

    protected MessageEditView(LocalMessageBase message)
        : base(null)
    {
        Message = message;
        RegenerateComponents();
    }

    public new MessageEditMenu Menu => (MessageEditMenu) base.Menu;

    public LocalMessageBase Message { get; }

    public bool ChangesSaved { get; protected set; }

    public void RegenerateComponents()
    {
        ClearComponents();

        AddComponent(ModifyEmbedIndexSelection);
        AddComponent(ModifyContentButton);
        AddComponent(ModifyEmbedColorButton);
        AddComponent(ModifyEmbedAuthorButton);
        AddComponent(ModifyEmbedTitleButton);
        AddComponent(ModifyEmbedDescriptionButton);
        AddComponent(ModifyEmbedImageUrlButton);
        AddComponent(ModifyEmbedThumbnailUrlButton);
        AddComponent(ModifyEmbedFooterButton);
        AddComponent(ModifyEmbedFieldSelection);
        AddComponent(SaveChangesButton);
    }

    public override void FormatLocalMessage(LocalMessageBase message)
    {
        /*
        if (message is LocalInteractionMessageResponse response)
            response.IsEphemeral = true;
        */

        message.Content = Message.Content.GetValueOrDefault();

        var modified = false;
        if (Message.Embeds.GetValueOrDefault() is {Count: > 0} embeds)
        {
            for (var i = 0; i < embeds.Count; i++)
            {
                var embed = embeds[i];
                // remove embed if length == 0
                if (embed.Length == 0)
                {
                    modified = true;
                    embeds.RemoveAt(i);
                    /*
                    embed.WithDescription($"{Markdown.Bold("WOAH THERE!!")} Embeds {Markdown.Underline("must")} have text to exist. \n" +
                                          "To avoid any issues, this embed was automatically given a description.");
                    */
                }

                if (embed.Fields.GetValueOrDefault() is { } fields)
                {
                    for (var j = 0; j < fields.Count; j++)
                    {
                        var field = fields[j];
                        // remove field if length == 0
                        if (field.Length == 0)
                        {
                            modified = true;
                            fields.RemoveAt(j);

                            /*
                            field.WithName("WOAH THERE!!")
                                .WithValue($"Fields {Markdown.Underline("must")} have text to exist. \n" +
                                           "To avoid any issues, this field was automatically given a name and value.");
                            */
                        }
                    }
                }
            }

            message.WithEmbeds(embeds);
        }

        if (modified)
            RegenerateComponents();

        if (_firstFormat && Message.Attachments.GetValueOrDefault()?.FirstOrDefault()?.Stream.GetValueOrDefault() is { CanSeek: true } stream )
        {
            stream.Seek(0, SeekOrigin.Begin);
            message.Attachments = Message.Attachments;
            _firstFormat = false;
        }

        base.FormatLocalMessage(message);
    }

    public async ValueTask ModifyEmbedIndexAsync(SelectionEventArgs e)
    {
        var index = int.Parse(e.SelectedOptions[0].Value.ToString());
        if (index == 0 && Message.Embeds.GetValueOrDefault() is not { Count: > 0 })
        {
            Message.Embeds = new List<LocalEmbed>
            {
                DefaultEmbed(index)
            };
        }
        else if (index >= 10)
        {
            await e.Interaction.Response().SendMessageAsync(new LocalInteractionMessageResponse()
                .WithIsEphemeral()
                .WithContent("Messages cannot exceed 10 embeds."));
        }
        else
        {
            while (index + 1 > Message.Embeds.GetValueOrDefault()?.Count)
            {
                Message.Embeds.Value.Add(DefaultEmbed(index));
            }
            
            _embedIndex = index;
        }

        RegenerateComponents();

        static LocalEmbed DefaultEmbed(int index)
        {
            return new LocalEmbed()
                .WithDescription($"New embed #{index + 1}! Embeds must have text to exist.\n" +
                                 "Feel free to remove this description once other text has been added.\n" +
                                 "(Removing all text from this embed will delete it!)");
        }
    }

    public ValueTask ModifyContentAsync(ButtonEventArgs e)
        => new(e.Interaction.Response().SendModalAsync(ModifyContentModal()));

    public ValueTask ModifyEmbedColorAsync(ButtonEventArgs e)
        => new(e.Interaction.Response().SendModalAsync(ModifyEmbedColorModal()));

    public ValueTask ModifyEmbedAuthorAsync(ButtonEventArgs e)
        => new(e.Interaction.Response().SendModalAsync(ModifyEmbedAuthorModal()));

    public ValueTask ModifyEmbedTitleAsync(ButtonEventArgs e)
        => new(e.Interaction.Response().SendModalAsync(ModifyEmbedTitleModal()));

    public ValueTask ModifyEmbedDescriptionAsync(ButtonEventArgs e)
        => new(e.Interaction.Response().SendModalAsync(ModifyEmbedDescriptionModal()));

    public ValueTask ModifyEmbedImageUrlAsync(ButtonEventArgs e)
        => new(e.Interaction.Response().SendModalAsync(ModifyEmbedImageUrlModal()));

    public ValueTask ModifyEmbedThumbnailUrlAsync(ButtonEventArgs e)
        => new(e.Interaction.Response().SendModalAsync(ModifyEmbedThumbnailUrlModal()));

    public ValueTask ModifyEmbedFooterAsync(ButtonEventArgs e)
        => new(e.Interaction.Response().SendModalAsync(ModifyEmbedFooterModal()));

    public async ValueTask ModifyEmbedFieldAsync(SelectionEventArgs e)
    {
        var index = int.Parse(e.SelectedOptions[0].Value.ToString());
        var embed = Message.Embeds.Value[_embedIndex];
        if (index == 0 && !embed.Fields.HasValue)
        {
            Message.Embeds.Value[_embedIndex].Fields = new List<LocalEmbedField>
            {
                DefaultField(index)
            };
        }
        else if (index >= 20)
        {
            await e.Interaction.Response().SendMessageAsync(new LocalInteractionMessageResponse()
                .WithIsEphemeral()
                .WithContent("Embeds cannot exceed 20 fields."));

            return;
        }
        else
        {
            while (index + 1 > embed.Fields.GetValueOrDefault()?.Count)
            {
                embed.Fields.Value.Add(DefaultField(index));
            }
        }

        _fieldIndex = index;
        await e.Interaction.Response().SendModalAsync(ModifyEmbedFieldModal());

        static LocalEmbedField DefaultField(int index)
        {
            return new LocalEmbedField()
                .WithName($"New field #{index + 1}!")
                .WithValue("Fields must have text to exist.\n" +
                           "Feel free to modify this value.\n" +
                           "(Removing all text from this field will delete it!)");
        }
    }

    // TODO: Timestamp

    public abstract ValueTask SaveChangesAsync(ButtonEventArgs e);

    /*
    public async Task ModifyMessageAsync(Action<ModifyWebhookMessageActionProperties> action)
    {
        var menu = (DefaultInteractionMenu) Menu;
        var response = await menu.Interaction.Followup().FetchResponseAsync();
        await menu.Interaction.Followup().ModifyAsync(response.Id, action);
    }
    */
    }