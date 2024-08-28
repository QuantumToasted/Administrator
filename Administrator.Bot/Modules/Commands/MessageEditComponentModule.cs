using Disqord;
using Disqord.Bot.Commands.Components;
using Qmmands;

namespace Administrator.Bot;

public sealed partial class MessageEditComponentModule
{
    [ModalCommand("Message:Modify:Content:*")]
    public partial Task<IResult> ModifyContent(Snowflake messageId, string? content = null);

    [ModalCommand("Embed:Modify:Color:*:*")]
    public partial Task<IResult> ModifyEmbedColor(int embedIndex, Snowflake messageId, Color? color = null);

    [ModalCommand("Embed:Modify:Author:*:*")]
    public partial Task<IResult> ModifyEmbedAuthor(int embedIndex, Snowflake messageId, string? name = null, string? iconUrl = null, string? url = null);

    [ModalCommand("Embed:Modify:Title:*:*")]
    public partial Task<IResult> ModifyEmbedTitle(int embedIndex, Snowflake messageId, string? title = null);

    [ModalCommand("Embed:Modify:Description:*:*")]
    public partial Task<IResult> ModifyEmbedDescription(int embedIndex, Snowflake messageId, string? description = null);

    [ModalCommand("Embed:Modify:ImageUrl:*:*")]
    public partial Task<IResult> ModifyEmbedImageUrl(int embedIndex, Snowflake messageId, string? imageUrl = null);

    [ModalCommand("Embed:Modify:ThumbnailUrl:*:*")]
    public partial Task<IResult> ModifyEmbedThumbnailUrl(int embedIndex, Snowflake messageId, string? thumbnailUrl = null);

    [ModalCommand("Embed:Modify:Footer:*:*")]
    public partial Task<IResult> ModifyEmbedFooter(int embedIndex, Snowflake messageId, string? text = null, string? iconUrl = null);

    [ModalCommand("Embed:Modify:Field:*:*:*")]
    public partial Task<IResult> ModifyEmbedField(int embedIndex, int fieldIndex, Snowflake messageId, string? name = null, string? value = null, bool? inline = null);
}