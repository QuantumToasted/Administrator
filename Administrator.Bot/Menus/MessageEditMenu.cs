using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using Disqord.Rest;
using Qommon;

namespace Administrator.Bot;

public sealed class MessageEditMenu(MessageEditView view, IUserInteraction interaction) 
    : AdminInteractionMenu(view, interaction)
{
    public DiscordBotBase Bot { get; } = (DiscordBotBase) interaction.Client;

    public override async ValueTask ApplyChangesAsync(InteractionReceivedEventArgs? e = null)
    {
        var view = View;
        if (view == null)
            return;

        var responseHelper = e?.Interaction.Response();
        if (HasChanges || view.HasChanges)
        {
            // If we have changes, we update the message accordingly.
            await view.UpdateAsync().ConfigureAwait(false);

            var localMessage = CreateLocalMessage();
            view.FormatLocalMessage(localMessage);

            try
            {
                if (responseHelper == null)
                {
                    // If there's no interaction provided, modify the message normally.
                    await Client.ModifyMessageAsync(ChannelId, MessageId, x =>
                    {
                        x.Content = localMessage.Content;
                        x.Embeds = Optional.Convert(localMessage.Embeds, embeds => embeds as IEnumerable<LocalEmbed>);
                        x.Components = Optional.Convert(localMessage.Components, components => components as IEnumerable<LocalRowComponent>);
                        x.AllowedMentions = localMessage.AllowedMentions;
                    }).ConfigureAwait(false);
                }
                else
                {
                    if (!responseHelper.HasResponded)
                    {
                        // If the user hasn't responded, respond to the interaction with modifying the message.
                        await responseHelper.ModifyMessageAsync(localMessage is LocalInteractionMessageResponse interactionMessageResponse
                            ? interactionMessageResponse
                            : new LocalInteractionMessageResponse
                            {
                                Content = localMessage.Content,
                                IsTextToSpeech = localMessage.IsTextToSpeech,
                                Embeds = localMessage.Embeds,
                                AllowedMentions = localMessage.AllowedMentions,
                                Components = localMessage.Components
                            }).ConfigureAwait(false);
                    }
                    else
                    {
                        // If the user deferred the response (a button is taking too long, for example), modify the message via a followup.
                        await e!.Interaction.Followup().ModifyResponseAsync(x =>
                        {
                            x.Content = localMessage.Content;
                            x.Embeds = Optional.Convert(localMessage.Embeds, embeds => embeds as IEnumerable<LocalEmbed>);
                            x.Components = Optional.Convert(localMessage.Components, components => components as IEnumerable<LocalRowComponent>);
                            x.AllowedMentions = localMessage.AllowedMentions;
                        }).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                HasChanges = false;
                // view.HasChanges = false;
            }
        }
        else if (responseHelper is { HasResponded: false })
        {
            // Acknowledge the interaction to prevent it from failing.
            await responseHelper.DeferAsync().ConfigureAwait(false);
        }
    }
}