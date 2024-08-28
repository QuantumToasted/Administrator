using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Qmmands;

namespace Administrator.Bot;

[SlashGroup("emoji")]
[RequireInitialAuthorPermissions(Permissions.ManageExpressions)]
public sealed partial class EmojiModule
{
    [SlashCommand("info")]
    [Description("Displays information for any emoji.")]
    public partial Task<IResult> DisplayInfo(
        [Description("The emoji to display information for. Can be any type of emoji.")]
            IEmoji emoji);

    [SlashCommand("create")]
    [Description("Creates a new server emoji from an image or GIF.")]
    [RequireBotPermissions(Permissions.ManageExpressions)]
    public partial Task<IResult> Create(
        [Description("The image or GIF for the new emoji.")]
        [Image]
        [MaximumAttachmentSize(256, FileSizeMeasure.KB)]
            IAttachment image,
        [Description("The name of the new emoji.")]
            string name);

    [SlashCommand("clone")]
    [Description("Clones an existing emoji into this server.")]
    [RequireBotPermissions(Permissions.ManageExpressions)]
    public partial Task<IResult> Clone(
        [Description("The emoji to clone from.")]
            ICustomEmoji emoji,
        [Description("The name for the newly created emoji. Defaults to the name of the emoji being cloned.")]
            string? newName = null);

    [SlashCommand("rename")]
    [Description("Renames an existing server emoji.")]
    [RequireBotPermissions(Permissions.ManageExpressions)]
    public partial Task<IResult> Rename(
        [Description("The emoji to rename.")] 
            IGuildEmoji emoji,
        [Description("The new name for the emoji.")]
            string newName);

    [SlashCommand("delete")]
    [Description("Deletes an existing server emoji.")]
    [RequireBotPermissions(Permissions.ManageExpressions)]
    public partial Task<IResult> Delete(
        [Description("The emoji to delete.")]
            IGuildEmoji emoji);
}