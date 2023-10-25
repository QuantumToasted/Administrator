using Disqord;
using Disqord.Extensions.Interactivity.Menus;
using Qommon;

namespace Administrator.Bot;

public abstract partial class MessageEditView
{
    public SelectionViewComponent ModifyEmbedIndexSelection
    {
        get
        {
            var selection = new SelectionViewComponent(ModifyEmbedIndexAsync)
            {
                Placeholder = "Select embed to modify...",
                Row = 0,
                Position = 0,
                MaximumSelectedOptions = 1,
                MinimumSelectedOptions = 1
            };

            if (Message.Embeds.GetValueOrDefault() is not { Count: > 0 } embeds)
            {
                selection.Placeholder = "Add an embed here...";
                selection.Options.Add(new LocalSelectionComponentOption("Add embed 1", "0"));
                return selection;
            }

            for (var i = 0; i < embeds.Count; i++)
            {
                if (i == 9) // max embeds is 25, but let's just limit it to something sane
                {
                    selection.Options.Add(new LocalSelectionComponentOption("Embed limit reached!", "10")); // TODO: special case > 10
                    break;
                }

                var labelText = i == _embedIndex ? $"Modifying embed {i + 1}" : $"Modify embed {i + 1}";
                selection.Options.Add(new LocalSelectionComponentOption(labelText, i.ToString()).WithIsDefault(i == _embedIndex));

                if (i == embeds.Count - 1)
                {
                    selection.Options.Add(new LocalSelectionComponentOption($"Add embed {i + 2}", (i + 1).ToString()));
                }
            }

            return selection;
        }
    }

    public ButtonViewComponent ModifyContentButton
    {
        get
        {
            var button = new ButtonViewComponent(ModifyContentAsync)
            {
                Label = "Add Content",
                Style = LocalButtonComponentStyle.Success,
                Row = 1,
                Position = 0
            };

            if (!string.IsNullOrWhiteSpace(Message.Content.GetValueOrDefault()))
            {
                button.Label = "Modify Content";
                button.Style = LocalButtonComponentStyle.Primary;
            };

            return button;
        }
    }

    public ButtonViewComponent ModifyEmbedColorButton
    {
        get
        {
            var button = new ButtonViewComponent(ModifyEmbedColorAsync)
            {
                Label = "Add Color",
                Style = LocalButtonComponentStyle.Success,
                Row = 1,
                Position = 1
            };

            if (Message.Embeds.GetValueOrDefault() is not {Count: > 0} embeds)
            {
                button.IsDisabled = true;
                return button;
            }

            var embed = embeds[_embedIndex];
            if (embed.Color.HasValue)
            {
                button.Label = "Modify Color";
                button.Style = LocalButtonComponentStyle.Primary;
            }

            return button;
        }
    }

    public ButtonViewComponent ModifyEmbedAuthorButton
    {
        get
        {
            var button = new ButtonViewComponent(ModifyEmbedAuthorAsync)
            {
                Label = "Add Author",
                Style = LocalButtonComponentStyle.Success,
                Row = 1,
                Position = 2
            };

            if (Message.Embeds.GetValueOrDefault() is not { Count: > 0 } embeds)
            {
                button.IsDisabled = true;
                return button;
            }

            var embed = embeds[_embedIndex];
            if (embed.Author.HasValue)
            {
                button.Label = "Modify Author";
                button.Style = LocalButtonComponentStyle.Primary;
            }

            return button;
        }
    }

    public ButtonViewComponent ModifyEmbedTitleButton
    {
        get
        {
            var button = new ButtonViewComponent(ModifyEmbedTitleAsync)
            {
                Label = "Add Title",
                Style = LocalButtonComponentStyle.Success,
                Row = 1,
                Position = 3
            };

            if (Message.Embeds.GetValueOrDefault() is not { Count: > 0 } embeds)
            {
                button.IsDisabled = true;
                return button;
            }

            var embed = embeds[_embedIndex];
            if (embed.Title.HasValue)
            {
                button.Label = "Modify Title";
                button.Style = LocalButtonComponentStyle.Primary;
            }

            return button;
        }
    }

    public ButtonViewComponent ModifyEmbedDescriptionButton
    {
        get
        {
            var button = new ButtonViewComponent(ModifyEmbedDescriptionAsync)
            {
                Label = "Add Description",
                Style = LocalButtonComponentStyle.Success,
                Row = 1,
                Position = 4
            };

            if (Message.Embeds.GetValueOrDefault() is not { Count: > 0 } embeds)
            {
                button.IsDisabled = true;
                return button;
            }

            var embed = embeds[_embedIndex];
            if (embed.Description.HasValue)
            {
                button.Label = "Modify Description";
                button.Style = LocalButtonComponentStyle.Primary;
            }

            return button;
        }
    }

    public ButtonViewComponent ModifyEmbedImageUrlButton
    {
        get
        {
            var button = new ButtonViewComponent(ModifyEmbedImageUrlAsync)
            {
                Label = "Add Image URL",
                Style = LocalButtonComponentStyle.Success,
                Row = 2,
                Position = 0
            };

            if (Message.Embeds.GetValueOrDefault() is not { Count: > 0 } embeds)
            {
                button.IsDisabled = true;
                return button;
            }

            var embed = embeds[_embedIndex];
            if (embed.ImageUrl.HasValue)
            {
                button.Label = "Modify Image URL";
                button.Style = LocalButtonComponentStyle.Primary;
            }

            return button;
        }
    }

    public ButtonViewComponent ModifyEmbedThumbnailUrlButton
    {
        get
        {
            var button = new ButtonViewComponent(ModifyEmbedThumbnailUrlAsync)
            {
                Label = "Add Thumbnail Image URL",
                Style = LocalButtonComponentStyle.Success,
                Row = 2,
                Position = 1
            };

            if (Message.Embeds.GetValueOrDefault() is not { Count: > 0 } embeds)
            {
                button.IsDisabled = true;
                return button;
            }

            var embed = embeds[_embedIndex];
            if (embed.ThumbnailUrl.HasValue)
            {
                button.Label = "Modify Thumbnail Image URL";
                button.Style = LocalButtonComponentStyle.Primary;
            }

            return button;
        }
    }

    public ButtonViewComponent ModifyEmbedFooterButton
    {
        get
        {
            var button = new ButtonViewComponent(ModifyEmbedFooterAsync)
            {
                Label = "Add Footer",
                Style = LocalButtonComponentStyle.Success,
                Row = 2,
                Position = 2
            };

            if (Message.Embeds.GetValueOrDefault() is not { Count: > 0 } embeds)
            {
                button.IsDisabled = true;
                return button;
            }

            var embed = embeds[_embedIndex];
            if (embed.Footer.HasValue)
            {
                button.Label = "Modify Footer";
                button.Style = LocalButtonComponentStyle.Primary;
            }

            return button;
        }
    }

    public SelectionViewComponent ModifyEmbedFieldSelection
    {
        get
        {
            var selection = new SelectionViewComponent(ModifyEmbedFieldAsync)
            {
                Placeholder = "Select field to modify...",
                Row = 3,
                Position = 0,
                MaximumSelectedOptions = 1,
                MinimumSelectedOptions = 1
            };

            if (Message.Embeds.GetValueOrDefault() is not { Count: > 0 } embeds)
            {
                selection.Options.Add(new LocalSelectionComponentOption("Add an embed first!", "0"));
                selection.IsDisabled = true;
                return selection;
            }

            var embed = embeds[_embedIndex];
            if (embed.Fields.GetValueOrDefault() is not { Count: > 0 } fields)
            {
                selection.Placeholder = "Add a field here...";
                selection.Options.Add(new LocalSelectionComponentOption("Add field 1", "0"));
                return selection;
            }

            for (var i = 0; i < fields.Count; i++)
            {
                if (i == 19) // max fields is 25, but let's just limit it to something sane
                {
                    selection.Options.Add(new LocalSelectionComponentOption("Field limit reached!", "20")); // TODO: special case > 20
                    break;
                }

                selection.Options.Add(new LocalSelectionComponentOption($"Modify field {i + 1}", i.ToString()).WithIsDefault(i == _fieldIndex));

                if (i == fields.Count - 1)
                {
                    selection.Options.Add(new LocalSelectionComponentOption($"Add field {i + 2}", (i + 1).ToString()));
                }
            }

            return selection;
        }
    }

    public ButtonViewComponent SaveChangesButton
    {
        get
        {
            var button = new ButtonViewComponent(SaveChangesAsync)
            {
                Label = "Save Changes",
                Style = LocalButtonComponentStyle.Secondary,
                Row = 4,
                Position = 0
            };

            return button;
        }
    }
}