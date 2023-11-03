using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Disqord.Rest;
using Qmmands;

namespace Administrator.Bot;

[SlashGroup("tag-admin")]
[RequireInitialAuthorPermissions(Permissions.ManageGuild)]
public sealed class TagAdminModule(TagService tags) : DiscordApplicationGuildModuleBase
{
    [SlashCommand("send")]
    [Description("Sends a tag to a specified channel.")]
    public async Task<IResult> SendAsync(
        [Description("The name of the tag to send.")]
            string name,
        [Description("The channel to send the tag to.")]
        [ChannelTypes(ChannelType.Text, ChannelType.PublicThread, ChannelType.PrivateThread)]
        [RequireAuthorChannelPermissions(Permissions.ViewChannels | Permissions.SendMessages)]
        [RequireBotChannelPermissions(Permissions.ViewChannels | Permissions.SendMessages)]
            IChannel channel)
    {
        name = name.ToLowerInvariant();

        var result = await tags.FindTagAsync(name);
        if (!result.IsSuccessful)
            return Response(result.ErrorMessage).AsEphemeral();

        await Deferral();
        var message = await result.Value.ToLocalMessageAsync<LocalMessage>(Context);
        await Bot.SendMessageAsync(channel.Id, message);

        return Response($"Tag \"{name}\" sent to {Mention.Channel(channel.Id)}!");
    }

    [SlashCommand("modify")]
    [Description("Modifies a user's tag.")]
    public async Task<IResult> ModifyAsync(
        [Description("The name of the tag to modify.")]
            string name)
    {
        name = name.ToLowerInvariant();

        var result = await tags.FindTagAsync(name);
        if (!result.IsSuccessful)
            return Response(result.ErrorMessage).AsEphemeral();
        
        var tag = result.Value;
        
        var message = await tag.ToLocalMessageAsync<LocalInteractionMessageResponse>();
        message.WithIsEphemeral();
        var view = new TagMessageEditView(tag.Name, message);
        return Menu(new MessageEditMenu(view, Context.Interaction));
    }

    [SlashCommand("delete")]
    [Description("Deletes any user's tag.")]
    public async Task<IResult> DeleteAsync(
        [Description("The name of the tag to delete.")]
            string name)
    {
        name = name.ToLowerInvariant();

        var result = await tags.DeleteTagAsync(name, false);
        if (!result.IsSuccessful)
            return Response(result.ErrorMessage).AsEphemeral();
        
        var tag = result.Value;

        return Response($"You've deleted {Mention.User(tag.OwnerId)}'s tag \"{name}\".");
    }

    [SlashCommand("transfer")]
    [Description("Force transfers a tag to another member, making them the owner.")]
    public async Task<IResult> TransferAsync(
        [Description("The name of the tag to transfer.")]
            string name,
        [Description("The new owner of the tag.")]
        [RequireNotBot]
            IMember newOwner)
    {
        name = name.ToLowerInvariant();

        var result = await tags.TransferTagAsync(name, newOwner, false);
        if (!result.IsSuccessful)
            return Response(result.ErrorMessage).AsEphemeral();
        
        return Response($"Tag \"{name}\" successfully transferred to {newOwner.Mention}.");
    }

    [AutoComplete("send")]
    [AutoComplete("modify")]
    [AutoComplete("delete")]
    [AutoComplete("transfer")]
    public Task AutoCompleteGuildTagsAsync(AutoComplete<string> name)
        => name.IsFocused ? tags.AutoCompleteTagsAsync(name) : Task.CompletedTask;
}