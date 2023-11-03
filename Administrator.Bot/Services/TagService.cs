using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Disqord.Bot.Commands.Interaction;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Extensions.Interactivity.Menus.Prompt;
using Humanizer;
using Microsoft.EntityFrameworkCore;

namespace Administrator.Bot;

[ScopedService]
public sealed class TagService(AdminDbContext db, ICommandContextAccessor contextAccessor, 
    AttachmentService attachments, SlashCommandMentionService mentions, AutoCompleteService autoComplete)
{
    private readonly IDiscordInteractionGuildCommandContext _context = (IDiscordInteractionGuildCommandContext)contextAccessor.Context;
    
    public async Task<Result<Tag>> CreateTagAsync(string name, IAttachment? attachment)
    {
        if (await db.Tags.FindAsync(_context.GuildId, name) is not null)
            return $"A tag already exists with the name \"{name}\"!";

        var guild = await db.GetOrCreateGuildConfigAsync(_context.GuildId);
        var tagCount = await db.Tags.CountAsync(x => x.GuildId == _context.GuildId && x.OwnerId == _context.AuthorId);
        if (tagCount >= guild.MaximumTagsPerUser)
            return $"You cannot create more than {"tag".ToQuantity(guild.MaximumTagsPerUser.Value)} in this server.";

        var tag = new Tag(_context.GuildId, _context.AuthorId, name)
        {
            Message = new JsonMessage
            {
                Content = $"New tag \"{name}\" created. Use the buttons below to modify its response.\n" +
                          $"(Attachments may not render correctly until you use {mentions.GetMention("tag show")}.)"
            }
        };
        
        if (attachment is not null && await attachments.GetAttachmentAsync(attachment.Url) is var fetchedAttachment)
        {
            tag.Attachment = new Attachment(fetchedAttachment.Stream.ToArray(), fetchedAttachment.FileName);
        }

        db.Tags.Add(tag);
        await db.SaveChangesAsync();

        return tag;
    }

    public async Task<Result<Tag>> FindTagAsync(string name, bool requireOwner = false)
    {
        if (await db.Tags.FindAsync(_context.GuildId, name) is not { } tag)
            return $"No tag exists with the name \"{name}\"!";
        
        if (requireOwner && tag.OwnerId != _context.AuthorId)
            return $"You don't own the tag \"{name}\"!";

        return tag;
    }

    public async Task<Result<Tag>> DeleteTagAsync(string name, bool requireOwner = true)
    {
        var findResult = await FindTagAsync(name);
        if (!findResult.IsSuccessful)
            return findResult.ErrorMessage;

        var tag = findResult.Value;
        if (requireOwner && tag.OwnerId != _context.AuthorId)
            return $"You don't own the tag \"{name}\"!";

        db.Tags.Remove(tag);
        await db.SaveChangesAsync();
        return tag;
    }

    public async Task<Result<Tag>> TransferTagAsync(string name, IMember newOwner, bool requireOwner = true)
    {
        var findResult = await FindTagAsync(name);
        if (!findResult.IsSuccessful)
            return findResult.ErrorMessage;
        
        var tag = findResult.Value;

        if (requireOwner)
        {
            if (tag.OwnerId != _context.AuthorId)
                return $"You don't own the tag \"{name}\"!";
        
            var view = new PromptView(x => x.WithContent($"Tag transfer started for tag \"{name}\". {newOwner.Mention}, you will need to confirm this transfer.")
                .WithAllowedMentions(new LocalAllowedMentions().WithUserIds(newOwner.Id)));

            await _context.Bot.RunMenuAsync(_context.ChannelId, new AdminInteractionMenu(view, _context.Interaction));

            if (!view.Result)
                return "The transfer request was denied or expired.";
        }

        tag.OwnerId = newOwner.Id;
        await db.SaveChangesAsync();
        return tag;
    }

    public async Task<Result<Tag>> ClaimTagAsync(string name)
    {
        var findResult = await FindTagAsync(name);
        if (!findResult.IsSuccessful)
            return findResult.ErrorMessage;
        
        var tag = findResult.Value;
        if (tag.OwnerId == _context.AuthorId)
            return $"You already own the tag \"{name}\"!";

        if (await _context.Bot.GetOrFetchMemberAsync(_context.GuildId, tag.OwnerId) is { } member)
            return $"{member.Mention} (the tag's owner) is still in this server!";

        tag.OwnerId = _context.AuthorId;
        await db.SaveChangesAsync();
        return tag;
    }

    public async Task AutoCompleteTagsAsync(AutoComplete<string> name, IUser? user = null)
    {
        var tags = await db.Tags.Where(x => x.GuildId == _context.GuildId).ToListAsync();

        if (user is not null)
            tags = tags.Where(x => x.OwnerId == _context.AuthorId).ToList();

        autoComplete.AutoComplete(name, tags);
    }
}