using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Disqord.Rest;
using Microsoft.EntityFrameworkCore;
using Qmmands;

namespace Administrator.Bot;

[SlashGroup("tag-admin")]
[RequireInitialAuthorPermissions(Permissions.ManageGuild)]
public sealed partial class TagAdminModule
{
    [SlashCommand("send")]
    [Description("Sends a tag to a specified channel.")]
    public partial Task<IResult> Send(
        [Description("The name of the tag to send.")]
        Tag tag,
        [Description("The channel to send the tag to.")]
        [ChannelTypes(ChannelType.Text, ChannelType.PublicThread, ChannelType.PrivateThread)]
        [RequireAuthorChannelPermissions(Permissions.ViewChannels | Permissions.SendMessages)]
        [RequireBotChannelPermissions(Permissions.ViewChannels | Permissions.SendMessages)]
        IChannel channel);

    [SlashCommand("modify")]
    [Description("Modifies a user's tag.")]
    public partial Task<IResult> Modify(
        [Description("The name of the tag to modify.")]
        Tag tag);

    [SlashCommand("delete")]
    [Description("Deletes any user's tag.")]
    public partial Task Delete(
        [Description("The name of the tag to delete.")]
        Tag tag);

    [SlashCommand("transfer")]
    [Description("Force transfers a tag to another member, making them the owner.")]
    public partial Task Transfer(
        [Description("The name of the tag to transfer.")]
        Tag tag,
        [Description("The new owner of the tag.")] 
        [RequireNotBot]
        IMember newOwner);

    [SlashCommand("link")]
    [Description("Links a tag to another via a button.")]
    public partial Task<IResult> Link(
        [Description("The tag to add a linked button to.")]
        Tag from,
        [Description("The tag which will be displayed when the button is clicked.")]
        Tag to,
        [Description("The text to show on the button. Defaults to name of the 'to' tag.")]
        string? text = null,
        [Description("The style for the button. Default: Primary")]
        LocalButtonComponentStyle style = LocalButtonComponentStyle.Primary,
        [Description("Whether clicking the button shows the linked tag only to the user who clicked it. Default: True")]
        bool ephemeral = true);

    [SlashCommand("unlink")]
    [Description("Unlinks a linked tag button from an existing tag.")]
    public partial Task<IResult> Unlink(
        [Description("The tag which is linked via a button.")]
        Tag to,
        [Description("The tag the button is added to.")]
        Tag from);

    [AutoComplete("send")]
    [AutoComplete("modify")]
    [AutoComplete("delete")]
    [AutoComplete("transfer")]
    public partial Task AutoCompleteTags(AutoComplete<string> tag);

    [AutoComplete("link")]
    [AutoComplete("unlink")]
    public partial Task AutoCompleteTagLinks(AutoComplete<string> from, AutoComplete<string> to);
    
    [SlashGroup("alias")]
    public sealed partial class TagAliasModule
    {
        [SlashCommand("add")]
        [Description("Adds an alias to a tag.")]
        public partial Task<IResult> Add(
            [Description("The name of the tag to create an alias for.")]
            Tag tag,
            [Description("The new alias for the tag.")] 
            [Maximum(Discord.Limits.Component.Button.MaxLabelLength)] 
            [Lowercase]
            string alias);

        [SlashCommand("remove")]
        [Description("Removes an alias from a tag.")]
        public partial Task<IResult> Remove(
            [Description("The name of the tag to create an alias for.")]
            Tag tag,
            [Description("The alias to be removed.")]
            string alias);

        [AutoComplete("add")]
        public partial Task AutoCompleteTags(AutoComplete<string> tag);

        [AutoComplete("remove")]
        public partial Task AutoCompleteTagAliases(AutoComplete<string> tag, AutoComplete<string> alias);
    }
}