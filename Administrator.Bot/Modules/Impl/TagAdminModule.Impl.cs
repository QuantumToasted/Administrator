using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Rest;
using Microsoft.EntityFrameworkCore;
using Qmmands;
using Qommon;
using Qommon.Collections;

namespace Administrator.Bot;

public sealed partial class TagAdminModule(AdminDbContext db, AutoCompleteService autoComplete) : DiscordApplicationGuildModuleBase
{
    private List<Tag>? _autoCompleteTags;
    
    public partial async Task<IResult> Send(Tag tag, IChannel channel)
    {
        await Deferral();
        await db.Tags.Entry(tag).Reference(x => x.Attachment).LoadAsync();
        var message = await tag.ToLocalMessageAsync<LocalMessage>(Bot, Context);
        await Bot.SendMessageAsync(channel.Id, message);

        return Response($"Tag \"{tag}\" sent to {Mention.Channel(channel.Id)}!");
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
        var prompt = new AdminPromptView($"Are you sure you want to delete {Mention.User(tag.OwnerId)} tag \"{tag}\"?")
            .OnConfirm($"You've deleted the tag \"{tag}\".");
        
        await View(prompt);

        if (prompt.Result)
        {
            db.Tags.Remove(tag);
            await db.SaveChangesAsync();
        }
    }

    public partial async Task Transfer(Tag tag, IMember newOwner)
    {
        var prompt = new AdminPromptView($"The tag \"{tag}\" will be transferred to {newOwner.Mention}.", flavorText: null)
            .OnConfirm($"The tag \"{tag}\" was successfully transferred to {newOwner.Mention}.");

        await View(prompt);

        if (prompt.Result)
        {
            tag.OwnerId = newOwner.Id;
            await db.SaveChangesAsync();
        }
    }

    public partial async Task<IResult> Link(Tag from, Tag to, string? text, LocalButtonComponentStyle style, bool ephemeral)
    {
        if (from.Name == to.Name)
            return Response("You cannot link a tag to itself!").AsEphemeral();
        
        if (await db.LinkedTags.AnyAsync(x => x.From == from.Name && x.To == to.Name))
            return Response($"Tags {Markdown.Bold(from)} and {Markdown.Bold(to)} are already linked!").AsEphemeral();

        var link = new TagLink(Context.GuildId, from.Name, to.Name, text, style, ephemeral); // using .Name to avoid alias messiness
        db.LinkedTags.Add(link);

        await db.SaveChangesAsync();

        return Response($"Tags {Markdown.Bold(from)} and {Markdown.Bold(to)} are now linked via a button.");
    }

    public partial async Task<IResult> Unlink(Tag to, Tag from)
    {
        if (await db.LinkedTags.FirstOrDefaultAsync(x => x.To == to.Name && x.From == from.Name) is not { } link)
            return Response($"The tag {Markdown.Bold(to)} is not linked to the tag {Markdown.Bold(from)}!").AsEphemeral();

        db.LinkedTags.Remove(link);
        await db.SaveChangesAsync();

        return Response($"The tag {Markdown.Bold(to)} is no longer linked to the tag {Markdown.Bold(from)}.");
    }

    public partial async Task AutoCompleteTags(AutoComplete<string> tag)
    {
        if (!tag.IsFocused)
            return;

        _autoCompleteTags ??= await db.Tags.Where(x => x.GuildId == Context.GuildId).OrderBy(x => x.Name).ToListAsync();
        autoComplete.AutoComplete(tag, _autoCompleteTags);
    }

    public partial Task AutoCompleteTagLinks(AutoComplete<string> from, AutoComplete<string> to)
    {
        return (from.IsFocused, to.IsFocused) switch
        {
            (true, false) => AutoCompleteTags(from),
            (false, true) => AutoCompleteTags(to),
            _ => Task.CompletedTask
        };
    }

    public sealed partial class TagAliasModule(AdminDbContext db, AutoCompleteService autoComplete) : DiscordApplicationGuildModuleBase
    {
        private List<Tag>? _autoCompleteTags;
        
        public partial async Task<IResult> Add(Tag tag, string alias)
        {
            if (tag.Name == alias)
                return Response("You cannot create an alias for a tag with the same text as its name!").AsEphemeral();

            if (await db.Tags.FirstOrDefaultAsync(x => x.GuildId == Context.GuildId && x.Aliases.Contains(alias)) is { } foundTag)
                return Response($"The tag \"{tag}\" already has the alias \"{alias}\"!").AsEphemeral();

            tag.Aliases = tag.Aliases.Append(alias).ToArray();
            await db.SaveChangesAsync();

            return Response($"\"{alias}\" has been added as an alias for the tag \"{tag}\".");
        }

        public partial async Task<IResult> Remove(Tag tag, string alias)
        {
            if (!tag.Aliases.Contains(alias))
                return Response($"\"{alias}\" isn't an alias for the tag \"{tag}\"!").AsEphemeral();

            tag.Aliases = tag.Aliases.Except([alias]).ToArray();
            await db.SaveChangesAsync();
            
            return Response($"\"{alias}\" has been removed from the alias list for the tag \"{tag}\".");
        }
        
        public partial async Task AutoCompleteTags(AutoComplete<string> tag)
        {
            if (!tag.IsFocused)
                return;

            _autoCompleteTags ??= await db.Tags.Where(x => x.GuildId == Context.GuildId)
                .OrderBy(x => x.Name)
                .ToListAsync();
            
            autoComplete.AutoComplete(tag, _autoCompleteTags);
        }

        public partial async Task AutoCompleteTagAliases(AutoComplete<string> tag, AutoComplete<string> alias)
        {
            _autoCompleteTags ??= await db.Tags.Where(x => x.GuildId == Context.GuildId)
                .OrderBy(x => x.Name)
                .ToListAsync();

            if (tag.IsFocused)
            {
                autoComplete.AutoComplete(tag, _autoCompleteTags);
                return;
            }

            if (!alias.IsFocused)
                return; // TODO: This shouldn't happen...right?

            var tagName = tag.Argument.GetValueOrDefault()?.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(tagName) || _autoCompleteTags.FirstOrDefault(x => x.Name == tagName) is not { } foundTag)
                return;
            
            alias.Choices.AddRange(foundTag.Aliases.Take(Discord.Limits.ApplicationCommand.Option.MaxChoiceAmount));
        }
    }
}