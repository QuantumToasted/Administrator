using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands.Components;
using Disqord.Rest;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Qmmands;

namespace Administrator.Bot;

public sealed partial class TagImportComponentModule(AdminDbContext db, SlashCommandMentionService mentions, AttachmentService attachments) : DiscordComponentGuildModuleBase
{
    public partial async Task<IResult> ImportAsync(Snowflake channelId, Snowflake messageId, string name)
    {
        await Deferral(true);

        IAttachment? attachment;
        IUserMessage fetchedMessage;
        try
        {
            var msg = await Bot.FetchMessageAsync(channelId, messageId);
            if (msg is not IUserMessage userMsg)
                return Response("Only user or bot messages can be imported as a tag.").AsEphemeral();

            fetchedMessage = userMsg;
            attachment = fetchedMessage.Attachments.FirstOrDefault();
        }
        catch (Exception ex)
        {
            return Response($"Failed to obtain the message for importing as a tag.\n{Markdown.CodeBlock(ex.Message)}").AsEphemeral();
        }

        name = name.ToLowerInvariant();

        if (await db.Tags.FirstOrDefaultAsync(x => x.GuildId == Context.GuildId && x.Name == name || x.Aliases.Contains(name)) is not null)
            return Response($"A tag already exists with the name or alias \"{name}\"!").AsEphemeral();

        var guild = await db.Guilds.GetOrCreateAsync(Context.GuildId);
        var tagCount = await db.Tags.CountAsync(x => x.GuildId == Context.GuildId && x.OwnerId == Context.AuthorId);

        if (tagCount >= guild.MaximumTagsPerUser)
            return Response($"You cannot create more than {"tag".ToQuantity(guild.MaximumTagsPerUser.Value)} in this server.").AsEphemeral();

        var response = new LocalInteractionMessageResponse()
            .WithContent($"New tag \"{name}\" created. Use the buttons below to modify its response.\n" +
                         $"(Attachments may not render correctly until you use {mentions.GetMention("tag show")}.)")
            .WithIsEphemeral();

        var tag = new Tag(Context.GuildId, Context.AuthorId, name)
        {
            Message = JsonMessage.FromMessage(response)
        };

        if (attachment is not null && await attachments.GetAttachmentAsync(attachment.Url) is var fetchedAttachment)
        {
            var tagAttachment = new Attachment(fetchedAttachment.FileName);
            if (await tagAttachment.UploadAsync(Bot, fetchedAttachment.Stream.ToArray()))
            {
                tag.Attachment = tagAttachment;
            }
        }

        db.Tags.Add(tag);
        await db.SaveChangesAsync();

        var view = new TagMessageEditView(tag.Name, response);
        return Menu(new MessageEditMenu(view, Context.Interaction), TimeSpan.FromMinutes(30));
    }
}