using Disqord;
using Disqord.Bot.Commands.Components;
using Disqord.Rest;
using Qommon;

namespace Administrator.Bot;

public sealed partial class AutoQuoteComponentModule : DiscordComponentGuildModuleBase
{
    public IComponentInteraction Interaction => (IComponentInteraction)Context.Interaction;

    public partial async Task Delete()
    {
        var embed = LocalEmbed.CreateFrom(Interaction.Message.Embeds[0])
            .WithCollectorsColor()
            .WithFooter($"Marked for deletion by {Context.Author.GetDisplayName()}", Context.Author.GetGuildAvatarUrl());

        await Interaction.Response().ModifyMessageAsync(new LocalInteractionMessageResponse()
            .WithEmbeds(embed)
            .WithComponents());
        
        //await Interaction.Message.ModifyAsync(x => x.Embeds = Optional.Create<IEnumerable<LocalEmbed>>([embed]));

        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(10));
            await Interaction.Message.DeleteAsync();
        });
    }
}