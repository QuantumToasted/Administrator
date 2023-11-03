using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Disqord.Rest;
using Microsoft.Extensions.Logging;
using Qmmands;
using IResult = Qmmands.IResult;

namespace Administrator.Bot;

[SlashGroup("emoji")]
[RequireInitialAuthorPermissions(Permissions.ManageExpressions)]
public sealed class EmojiModule(AttachmentService attachmentService, EmojiService emojiService, AdminDbContext db)
    : DiscordApplicationGuildModuleBase
{
    [SlashCommand("info")]
    [Description("Displays information for any emoji.")]
    public async Task<IResult> DisplayInfoAsync(
        [Description("The emoji to display information for. Can be any type of emoji.")]
            IEmoji emoji)
    {
        var message = new LocalInteractionMessageResponse();
        var embed = new LocalEmbed()
            .WithUnusualColor();

        switch (emoji)
        {
            case MappedEmoji mappedEmoji:
            {
                await Deferral();
                embed.WithTitle($"Info for emoji \\:{mappedEmoji.PrimaryName}:")
                    .WithFooter("This is a default Discord emoji.");

                if (mappedEmoji.NamesWithColons.Length > 1)
                {
                    embed.AddField("Alternate names",
                        string.Join('\n', mappedEmoji.NamesWithColons[1..].Select(Markdown.Code)));
                }

                var image = await emojiService.GetOrCreateDefaultEmojiAsync(mappedEmoji);
                embed.WithThumbnailUrl($"attachment://{mappedEmoji.PrimaryName}.png");
                message.AddAttachment(new LocalAttachment(image, $"{mappedEmoji.PrimaryName}.png"));
                break;
            }
            case IGuildEmoji guildEmoji:
            {
                await Deferral();
                embed.WithTitle($"Info for server emoji :{guildEmoji.Name}:")
                    .WithThumbnailUrl(guildEmoji.GetUrl(CdnAssetFormat.Automatic, 512))
                    .AddField("ID", guildEmoji.Id)
                    .AddField("Created", Markdown.Timestamp(guildEmoji.CreatedAt(), Markdown.TimestampFormat.RelativeTime))
                    .WithFooter("This emoji is from this server.");

                var emojiStats = await db.GetOrCreateEmojiStatisticsAsync(Context.GuildId, guildEmoji.Id);
                if (emojiStats.Uses > 0)
                    embed.AddField("Times used", emojiStats.Uses.ToString("N"));
                break;
            }
            case ICustomEmoji customEmoji:
            {
                embed.WithTitle($"Info for custom emoji :{customEmoji.Name}:")
                    .WithThumbnailUrl(customEmoji.GetUrl(CdnAssetFormat.Automatic, 512))
                    .AddField("ID", customEmoji.Id)
                    .AddField("Created", Markdown.Timestamp(customEmoji.CreatedAt(), Markdown.TimestampFormat.RelativeTime))
                    .WithFooter("This emoji is from another server.");
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(emoji));
        }

        return Response(message.AddEmbed(embed));
    }

    [SlashCommand("create")]
    [Description("Creates a new server emoji from an image or GIF.")]
    [RequireBotPermissions(Permissions.ManageExpressions)]
    public async Task<IResult> CreateAsync(
        [Description("The image or GIF for the new emoji.")]
        [Image]
        [MaximumAttachmentSize(256, FileSizeMeasure.KB)]
            IAttachment image,
        [Description("The name of the new emoji.")]
            string name)
    {
        var attachment = await attachmentService.GetAttachmentAsync(image.Url);

        try
        {
            var newEmoji = await Bot.CreateGuildEmojiAsync(Context.GuildId, name, attachment.Stream);
            return Response($"New emoji {Markdown.Code($":{newEmoji.Name}:")} created! {newEmoji.Tag}");
        }
        catch (RestApiException ex)
        {
            Logger.LogError(ex, "Unable to create emoji in guild {GuildId}.", Context.GuildId.RawValue);
            return Response("An error occurred while attempting to create a new emoji.\n" +
                            "One of several things may be the cause - invalid image types, no free emoji slots, too-large images, etc.\n" +
                            "The following text may be of assistance:\n" +
                            Markdown.CodeBlock(ex.Message)).AsEphemeral();
        }
    }

    [SlashCommand("clone")]
    [Description("Clones an existing emoji into this server.")]
    [RequireBotPermissions(Permissions.ManageExpressions)]
    public async Task<IResult> CloneAsync(
        [Description("The emoji to clone from.")]
            ICustomEmoji emoji,
        [Description("The name for the newly created emoji. Defaults to the name of the emoji being cloned.")]
            string? newName = null)
    {
        newName ??= emoji.Name!;
        var attachment = await attachmentService.GetAttachmentAsync(emoji.GetUrl());

        try
        {
            var newEmoji = await Bot.CreateGuildEmojiAsync(Context.GuildId, newName, attachment.Stream);
            return Response($"Emoji {emoji.Tag} cloned! New emoji: {newEmoji.Tag}");
        }
        catch (RestApiException ex)
        {
            Logger.LogError(ex, "Unable to clone emoji {Emoji} in guild {GuildId}.", emoji.Tag, Context.GuildId.RawValue);
            return Response("An error occurred while attempting to clone this emoji.\n" +
                            "One of several things may be the cause - invalid image types, no free emoji slots, too-large images, etc.\n" +
                            "The following text may be of assistance:\n" +
                            Markdown.CodeBlock(ex.Message)).AsEphemeral();
        }
    }

    [SlashCommand("rename")]
    [Description("Renames an existing server emoji.")]
    [RequireBotPermissions(Permissions.ManageExpressions)]
    public async Task<IResult> DeleteAsync(
        [Description("The emoji to rename.")]
            IGuildEmoji emoji,
        [Description("The new name for the emoji.")]
            string newName)
    {
        await emoji.ModifyAsync(x => x.Name = newName);
        return Response($"Emoji {Markdown.Code($":{emoji.Name}:")} deleted.");
    }

    [SlashCommand("delete")]
    [Description("Deletes an existing server emoji.")]
    [RequireBotPermissions(Permissions.ManageExpressions)]
    public async Task<IResult> DeleteAsync(
        [Description("The emoji to delete.")] 
            IGuildEmoji emoji)
    {
        await emoji.DeleteAsync();
        return Response($"Emoji {Markdown.Code($":{emoji.Name}:")} deleted.");
    }
}