using Disqord;
using Disqord.Bot.Commands.Application;
using Qmmands;
using Disqord.Bot.Commands;

namespace Administrator.Bot;

[SlashGroup("tag")]
public sealed class TagModule(TagService tags) 
    : DiscordApplicationGuildModuleBase
{
    [SlashCommand("create")]
    [Description("Creates a new tag.")]
    public async Task<IResult> CreateAsync(
        [Maximum(50)]
        [Description("The name of the tag. Must not exist on this server.")]
            string name,
        [NonNitroAttachment]
        [Description("The attachment this tag should respond with.")]
            IAttachment? attachment = null)
    {
        name = name.ToLowerInvariant();

        var result = await tags.CreateTagAsync(name, attachment);
        if (!result.IsSuccess)
            return Response(result.ErrorMessage).AsEphemeral();

        var tag = result.Value;
        var message = await tag.ToLocalMessageAsync<LocalInteractionMessageResponse>(Context);
        message.WithIsEphemeral();
        
        var view = new TagMessageEditView(tag.Name, message);
        return Menu(new MessageEditMenu(view, Context.Interaction));
    }

    [SlashCommand("show")]
    [Description("Recalls a specific tag and sends it.")]
    public async Task<IResult> ShowAsync(
        [Description("The name of the tag to send.")]
            string name)
    {
        name = name.ToLowerInvariant();

        var result = await tags.FindTagAsync(name);
        if (!result.IsSuccess)
            return Response(result.ErrorMessage).AsEphemeral();

        var tag = result.Value;
        
        await Deferral();
        var message = await tag.ToLocalMessageAsync<LocalInteractionMessageResponse>(Context);
        return Response(message);
    }

    [SlashCommand("info")]
    [Description("Displays information for a specific tag.")]
    public async Task<IResult> DisplayInfoAsync(
        [Description("The name of the tag to display info for.")]
            string name)
    {
        name = name.ToLowerInvariant();

        var result = await tags.FindTagAsync(name);
        if (!result.IsSuccess)
            return Response(result.ErrorMessage).AsEphemeral();

        var tag = result.Value;

        return Response(tag.FormatInfoEmbed(Bot));
    }

    [SlashCommand("modify")]
    [Description("Modifies one of your tags.")]
    public async Task<IResult> ModifyAsync(
        [Description("The name of the tag to modify.")] 
            string name)
    {
        name = name.ToLowerInvariant();
        
        var result = await tags.FindTagAsync(name);
        if (!result.IsSuccess)
            return Response(result.ErrorMessage).AsEphemeral();

        var tag = result.Value;
        
        var message = await tag.ToLocalMessageAsync<LocalInteractionMessageResponse>();
        message.WithIsEphemeral();
        var view = new TagMessageEditView(tag.Name, message);
        return Menu(new MessageEditMenu(view, Context.Interaction));
    }

    [SlashCommand("delete")]
    [Description("Deletes one of your tags.")]
    public async Task<IResult> DeleteAsync(
        [Description("The name of the tag to delete.")]
            string name)
    {
        name = name.ToLowerInvariant();
        
        var result = await tags.DeleteTagAsync(name);
        if (!result.IsSuccess)
            return Response(result.ErrorMessage).AsEphemeral();

        return Response($"You've deleted your tag \"{name}\".");
    }

    [SlashCommand("transfer")]
    [Description("Transfers a tag to another member, making them the owner.")]
    public async Task<IResult> TransferAsync(
        [Description("The name of the tag to transfer.")]
            string name,
        [Description("The new owner of the tag.")]
        [RequireNotBot]
        [RequireNotAuthor]
            IMember newOwner)
    {
        name = name.ToLowerInvariant();

        var result = await tags.TransferTagAsync(name, newOwner);
        if (!result.IsSuccess)
            return Response(result.ErrorMessage).AsEphemeral();

        return Response($"Tag \"{name}\" successfully transferred to {newOwner.Mention}.");
    }
    
    [SlashCommand("claim")]
    [Description("Claims a dormant tag of a member who has left the server.")]
    public async Task<IResult> ClaimAsync(
        [Description("The name of the tag to claim.")]
            string name)
    {
        name = name.ToLowerInvariant();

        var result = await tags.ClaimTagAsync(name);
        if (!result.IsSuccess)
            return Response(result.ErrorMessage).AsEphemeral();
        
        return Response($"You've successfully claimed the dormant tag \"{name}\".");
    }

    [AutoComplete("delete")]
    [AutoComplete("transfer")]
    [AutoComplete("modify")]
    public async Task AutoCompleteUserTagsAsync(
        AutoComplete<string> name)
    {
        if (!name.IsFocused)
            return;

        await tags.AutoCompleteTagsAsync(name, Context.Author);
    }
    
    [AutoComplete("claim")]
    [AutoComplete("show")]
    public async Task AutoCompleteAllTagsAsync(
        AutoComplete<string> name)
    {
        if (!name.IsFocused)
            return;

        await tags.AutoCompleteTagsAsync(name);
    }
}