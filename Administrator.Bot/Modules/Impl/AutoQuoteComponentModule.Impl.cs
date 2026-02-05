using Disqord;
using Disqord.Bot.Commands.Components;
using Disqord.Rest;

namespace Administrator.Bot;

public sealed partial class AutoQuoteComponentModule : DiscordComponentGuildModuleBase
{
    public IComponentInteraction Interaction => (IComponentInteraction)Context.Interaction;

    public partial async Task Delete()
    {
        var prompt = new AdminPromptView("You are about to delete this auto-quote message for ALL users.", isEphemeral: true)
            .OnConfirm("Auto-quote deletion started.");

        await View(prompt);

        if (!prompt.Result)
            return;
        
        var embed = LocalEmbed.CreateFrom(Interaction.Message.Embeds[0])
            .WithCollectorsColor()
            .WithFooter($"Marked for removal by {Context.Author.GetDisplayName()}", Context.Author.GetGuildAvatarUrl());

        await Interaction.Message.ModifyAsync(x =>
        {
            x.Embeds = new[] { embed };
            x.Components = Array.Empty<LocalComponent>();
        });

        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(10));
            await Interaction.Message.DeleteAsync();
        });
    }
}