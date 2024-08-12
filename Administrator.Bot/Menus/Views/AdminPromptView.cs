using System.Text;
using Disqord;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Extensions.Interactivity.Menus.Prompt;
using Disqord.Rest;

namespace Administrator.Bot;

public class AdminPromptView : PromptView
{
    private const string DEFAULT_FLAVOR_TEXT = "This action CANNOT be undone.";
    
    public AdminPromptView(Action<LocalMessageBase> messageTemplate)
        : base(messageTemplate)
    {
        MessageTemplate = messageTemplate;
    }
    
    public AdminPromptView(string prompt, LocalEmbed? embed = null, bool isEphemeral = false, string? flavorText = DEFAULT_FLAVOR_TEXT)
        : base(null!)
    {
        var contentBuilder = new StringBuilder()
            .AppendNewline(prompt);

        if (!string.IsNullOrWhiteSpace(flavorText))
            contentBuilder.AppendNewline(flavorText);
        
        MessageTemplate = x =>
        {
            x.WithContent(contentBuilder.ToString());
            if (embed is not null)
                x.AddEmbed(embed);
            (x as LocalInteractionMessageResponse)?.WithIsEphemeral(isEphemeral);
        };
    }
    
    public string? ConfirmMessage { get; private set; }

    public AdminPromptView OnConfirm(string message)
    {
        ConfirmMessage = message;
        return this;
    }

    public AdminPromptView OnAbort(string message)
    {
        AbortMessage = message;
        return this;
    }

    protected override async ValueTask CompleteAsync(bool result, ButtonEventArgs e)
    {
        Result = result;

        Task task;
        if (result)
        {
            task = !string.IsNullOrWhiteSpace(ConfirmMessage)
                ? e.Interaction.Response().ModifyMessageAsync(new LocalInteractionMessageResponse().WithContent(ConfirmMessage).WithEmbeds().WithComponents())
                : Menu.Client.DeleteMessageAsync(Menu.ChannelId, Menu.MessageId);
        }
        else
        {
            task = !string.IsNullOrWhiteSpace(AbortMessage)
                ? e.Interaction.Response().ModifyMessageAsync(new LocalInteractionMessageResponse().WithContent(AbortMessage).WithEmbeds().WithComponents())
                : Menu.Client.DeleteMessageAsync(Menu.ChannelId, Menu.MessageId);
        }

        try
        {
            await task;
        }
        finally
        {
            Menu.Stop();
        }
    }
}