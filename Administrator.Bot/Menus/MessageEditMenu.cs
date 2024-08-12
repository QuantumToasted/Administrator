using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using Microsoft.Extensions.DependencyInjection;

namespace Administrator.Bot;

public sealed class MessageEditMenu(MessageEditView view, IUserInteraction interaction) 
    : AdminInteractionMenu(view, interaction)
{
    public DiscordBotBase Bot { get; } = (DiscordBotBase) interaction.Client;
    
    public InteractionReceivedEventArgs? LastEventArgs { get; private set; }

    public override LocalMessageBase CreateLocalMessage()
    {
        var baseMessage = base.CreateLocalMessage();
        if (view.Message is LocalInteractionMessageResponse { IsEphemeral: true })
        {
            (baseMessage as LocalInteractionMessageResponse)?.WithIsEphemeral();
        }

        return baseMessage;
    }

    protected override async ValueTask<Snowflake> InitializeAsync(CancellationToken cancellationToken)
    {
        var id = await base.InitializeAsync(cancellationToken);
        var bot = (DiscordBotBase) Client;
        var service = bot.Services.GetRequiredService<MessageEditViewService>();
        service.Views[id] = (MessageEditView) View!;
        return id;
    }

    protected override ValueTask HandleInteractionAsync(InteractionReceivedEventArgs e)
    {
        LastEventArgs = e;
        return base.HandleInteractionAsync(e);
    }

    /* TODO: WTF was the point of this? it breaks the view for some reason. All that was changed from the source was commenting out view.HasChanges = false
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
    */
}