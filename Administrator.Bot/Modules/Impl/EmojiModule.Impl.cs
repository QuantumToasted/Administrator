using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Rest;
using Microsoft.Extensions.Logging;
using Qmmands;

namespace Administrator.Bot;

public sealed partial class EmojiModule(AttachmentService attachmentService, EmojiService emojiService, AdminDbContext db) : DiscordApplicationGuildModuleBase
{
    public partial async Task<IResult> DisplayInfo(IEmoji emoji)
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

                var emojiStats = await db.EmojiStats.GetOrCreateAsync(Context.GuildId, guildEmoji.Id);
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

    public partial async Task<IResult> Create(IAttachment image, string name)
    {
        name = name.Replace(":", "");
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

    public partial async Task<IResult> Clone(ICustomEmoji emoji, string? newName)
    {
        newName ??= emoji.Name!;
        newName = newName.Replace(":", "");
        var attachment = await attachmentService.GetAttachmentAsync(emoji.GetUrl());

        try
        {
            var newEmoji = await Bot.CreateGuildEmojiAsync(Context.GuildId, newName, attachment.Stream);
            return Response($"Emoji {Markdown.Code(emoji.Tag)} cloned! New emoji: {newEmoji.Tag}");
        }
        catch (RestApiException ex)
        {
            Logger.LogWarning(ex, "Unable to clone emoji {Emoji} in guild {GuildId}.", emoji.Tag, Context.GuildId.RawValue);
            return Response("An error occurred while attempting to clone this emoji.\n" +
                            "One of several things may be the cause - invalid image types, no free emoji slots, too-large images, etc.\n" +
                            "The following text may be of assistance:\n" +
                            Markdown.CodeBlock(ex.Message)).AsEphemeral();
        }
    }

    public partial async Task<IResult> Rename(IGuildEmoji emoji, string newName)
    {
        var oldName = emoji.Name;
        newName = newName.Replace(":", "");
        await emoji.ModifyAsync(x => x.Name = newName);
        return Response($"Emoji {Markdown.Code($":{oldName}:")} renamed to {Markdown.Code($":{newName}:")}.");
    }

    public partial async Task<IResult> Delete(IGuildEmoji emoji)
    {
        await emoji.DeleteAsync();
        return Response($"Emoji {Markdown.Code($":{emoji.Name}:")} deleted.");
    }
}