using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands.Application;
using Qmmands;
using Disqord.Bot.Commands;

namespace Administrator.Bot;

[SlashGroup("tag")]
public sealed partial class TagModule
{
    [SlashCommand("list")]
    [Description("Lists all tags in this server.")]
    public partial Task<IResult> List();

    [SlashCommand("create")]
    [Description("Creates a new tag.")]
    public partial Task<IResult> Create(
        [Description("The name of the tag. Must not exist on this server.")]
        [Maximum(Discord.Limits.Component.Button.MaxLabelLength)] 
        [Lowercase]
        string name,
        [Description("The attachment this tag should respond with.")]
        [NonNitroAttachment] 
        IAttachment? attachment = null);

    [SlashCommand("show")]
    [Description("Recalls a specific tag and sends it.")]
    public partial Task Show(
        [Description("The name of the tag to send.")]
        Tag tag,
        [Description("If True, this tag will only be shown to you.")]
        bool ephemeral = false);

    [SlashCommand("info")]
    [Description("Displays information for a specific tag.")]
    public partial Task<IResult> Info(
        [Description("The name of the tag to display info for.")]
        Tag tag);

    [SlashCommand("modify")]
    [Description("Modifies one of your tags.")]
    public partial Task<IResult> Modify(
        [Description("The name of the tag to modify.")]
        [RequireTagOwner]
        Tag tag);

    [SlashCommand("delete")]
    [Description("Deletes one of your tags.")]
    public partial Task Delete(
        [Description("The name of the tag to delete.")] 
        [RequireTagOwner]
        Tag tag);

    [SlashCommand("transfer")]
    [Description("Transfers a tag to another member, making them the owner.")]
    public partial Task Transfer(
        [Description("The name of the tag to transfer.")]
        [RequireTagOwner]
        Tag tag,
        [Description("The new owner of the tag.")] 
        [RequireNotBot] 
        [RequireNotAuthor]
        IMember newOwner);

    [SlashCommand("claim")]
    [Description("Claims a dormant tag of a member who has left the server.")]
    public partial Task<IResult> Claim(
        [Description("The name of the tag to claim.")]
        Tag tag);

    [AutoComplete("delete")] // user
    [AutoComplete("transfer")]
    [AutoComplete("modify")]
    [AutoComplete("claim")] // all
    [AutoComplete("show")]
    [AutoComplete("info")]
    public partial Task AutoCompleteTags(AutoComplete<string> tag);
}