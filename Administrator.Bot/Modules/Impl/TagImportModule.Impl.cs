using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Rest;

namespace Administrator.Bot;

public sealed partial class TagImportModule : DiscordApplicationGuildModuleBase
{
    public partial async Task ConvertToTag(IMessage message)
    {
        var modal = new LocalInteractionModalResponse()
            .WithCustomId($"Tag:Import:{message.ChannelId}:{message.Id}")
            .WithTitle("Enter the name for this imported tag.")
            .AddComponent(new LocalRowComponent().AddComponent(new LocalTextInputComponent()
                .WithCustomId("name")
                .WithLabel("Name")
                .WithStyle(TextInputComponentStyle.Short)
                .WithMaximumInputLength(Discord.Limits.Component.Button.MaxLabelLength)
                .WithIsRequired()));

        await Context.Interaction.Response().SendModalAsync(modal);
    }
}