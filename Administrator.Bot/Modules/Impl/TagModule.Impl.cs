using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Extensions.Interactivity.Menus.Paged;
using Disqord.Gateway;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Qmmands;
using Qommon.Metadata;

namespace Administrator.Bot;

public sealed partial class TagModule(AdminDbContext db, AttachmentService attachments, AutoCompleteService autoComplete, SlashCommandMentionService mentions)
    : DiscordApplicationGuildModuleBase
{
    private List<Tag>? _autoCompleteTags;
    
    public partial async Task<IResult> List()
    {
        var guildTags = await db.Tags.Where(x => x.GuildId == Context.GuildId).OrderBy(x => x.Name).ToListAsync();
        var guild = Bot.GetGuild(Context.GuildId)!;
        var pages = guildTags.Chunk(25)
            .Select(chunk =>
            {
                var embed = new LocalEmbed()
                    .WithUnusualColor()
                    .WithTitle($"All tags in {guild.Name}")
                    .WithDescription(string.Join('\n', chunk.Select(x => x.Name)));
                
                return new Page().AddEmbed(embed);
            }).ToList();
        
        return pages.Count switch
        {
            0 => Response("No tags have been created on this server!").AsEphemeral(),
            1 => Response(pages[0].Embeds.Value[0]),
            _ => Menu(new AdminInteractionMenu(new AdminPagedView(pages), Context.Interaction))
        };
    }
    
    public partial async Task<IResult> Create(string name, IAttachment? attachment)
    {
        await Deferral(true);

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

    public partial async Task<IResult> Modify(Tag tag)
    {
        var message = await tag.ToLocalMessageAsync<LocalInteractionMessageResponse>(Bot);
        message.WithIsEphemeral();
        var view = new TagMessageEditView(tag.Name, message);
        return Menu(new MessageEditMenu(view, Context.Interaction), TimeSpan.FromMinutes(30));
    }

    public partial async Task Delete(Tag tag)
    {
        var prompt = new AdminPromptView($"Are you sure you want to delete your tag \"{tag}\"?")
            .OnConfirm($"You've deleted your tag \"{tag}\".");

        await View(prompt);

        if (prompt.Result)
        {
            db.Tags.Remove(tag);
            await db.SaveChangesAsync();
        }
    }

    public partial async Task Show(Tag tag, bool ephemeral)
    {
        await db.Tags.Entry(tag).Reference(x => x.Attachment).LoadAsync();
        
        tag.Uses++;
        tag.LastUsedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();
        await tag.ShowAsync(Context, ephemeral);
    }

    public partial async Task<IResult> Info(Tag tag)
    {
        var linkedTags = await db.LinkedTags.Where(x => x.GuildId == Context.GuildId && x.From == tag.Name).ToListAsync();
        return Response(tag.FormatInfoEmbed(Bot, linkedTags));
    }

    public partial async Task Transfer(Tag tag, IMember newOwner)
    {
        var prompt = new AdminPromptView(x => x.WithContent($"Tag transfer started for tag \"{tag}\". {newOwner.Mention}, you will need to confirm this transfer.")
                .WithAllowedMentions(new LocalAllowedMentions().WithUserIds(newOwner.Id)))
            .OnConfirm($"{Context.Author.Mention}, your tag \"{tag}\" was successfully transferred to {newOwner.Mention}.")
            .OnAbort("The transfer request was denied or expired.");

        await Menu(new AdminInteractionMenu(prompt, Context.Interaction) { AuthorId = newOwner.Id });

        if (prompt.Result)
        {
            tag.OwnerId = newOwner.Id;
            await db.SaveChangesAsync();
        }
    }

    public partial async Task<IResult> Claim(Tag tag)
    {
        if (tag.OwnerId == Context.AuthorId)
            return Response($"You already own the tag \"{tag}\"!").AsEphemeral();

        if (await Context.Bot.GetOrFetchMemberAsync(Context.GuildId, tag.OwnerId) is { } member)
            return Response($"{member.Mention} (the tag's owner) is still in this server!").AsEphemeral();

        tag.OwnerId = Context.AuthorId;
        await db.SaveChangesAsync();

        return Response($"You've successfully claimed the dormant tag \"{tag}\".");
    }

    public partial async Task AutoCompleteTags(AutoComplete<string> tag)
    {
        if (!tag.IsFocused)
            return;
        
        _autoCompleteTags ??= await db.Tags.Where(x => x.GuildId == Context.GuildId)
            .OrderBy(x => x.Name)
            .ToListAsync();
        
        var command = Context.GetMetadata<ApplicationCommand>("OriginalCommand")!;
        var parameter = command.Parameters.First(x => x.Name == nameof(tag));
        if (parameter.Checks.OfType<RequireTagOwnerAttribute>().Any())
        {
            autoComplete.AutoComplete(tag, _autoCompleteTags.Where(x => x.OwnerId == Context.AuthorId).ToList());
            return;
        }
        
        autoComplete.AutoComplete(tag, _autoCompleteTags);
    }
}