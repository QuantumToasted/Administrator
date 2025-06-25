using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Qmmands;

namespace Administrator.Bot;

[SlashGroup("button-role")]
[RequireInitialAuthorPermissions(Permissions.ManageRoles)]
public sealed partial class ButtonRoleModule
{
    [SlashCommand("list")]
    [Description("Lists all button roles for this server.")]
    public partial Task<IResult> List();

    [SlashCommand("create")]
    [Description("Creates a new button role, granting a role when the button is pressed on a message.")]
    public partial Task<IResult> Create(
        [Description("The role to grant.")]
        [RequireAuthorRoleHierarchy]
        [RequireBotRoleHierarchy]
            IRole role,
        [Description("The channel the message is in.")]
        [ChannelTypes(ChannelType.Text)]
        [RequireBotChannelPermissions(Permissions.ViewChannels)]
            IChannel channel,
        [Description("The ID of the message to add the button to.")]
            Snowflake messageId,
        [Description("The text to display on the button. Optional if an emoji is set.")]
        [Maximum(Discord.Limits.Component.Button.MaxLabelLength)]
            string? text = null,
        [Description("The emoji to display on the button. Optional if text is set.")]
            IEmoji? emoji = null,
        [Description("The style of the button.")]
            LocalButtonComponentStyle style = LocalButtonComponentStyle.Primary,
        [Description("The row (1-5) the button will be on. Default: Automatic")]
        [Range(1, 5)]
            int? row = null,
        [Description("The position on the row the button will be on. Default: Automatic")]
        [Range(1, 5)]
            int? position = null,
        [Description("Only one role from this group ID can be selected at a time.")]
        [Minimum(1)]
            int? exclusiveGroup = null);

    [SlashCommand("modify")]
    [Description("Modifies an existing button role.")]
    public partial Task<IResult> Modify(
        [Name("button-role")] 
        [Description("The ID of the button to remove.")]
            int buttonRoleId,
        [Description("The new role this button will assign.")]
            IRole? role = null,
        [Description("The new text for this button role.")]
            string? text = null,
        [Description("The ID of the button to remove.")]
            IEmoji? emoji = null,
        [Description("The ID of the button to remove.")]
            LocalButtonComponentStyle? style = null);

    [SlashCommand("remove")]
    [Description("Removes an existing button role.")]
    public partial Task<IResult> Remove(
        [Name("button-role")] 
        [Description("The ID of the button to remove.")]
            int buttonRoleId);

    [AutoComplete("modify")]
    [AutoComplete("remove")]
    public partial Task AutoCompleteButtonRoles(AutoComplete<int> buttonRole);
}