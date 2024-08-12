using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Extensions.Interactivity.Menus;

namespace Administrator.Bot;

public class GreetingMessageEditView : MessageEditView
{
    public GreetingMessageEditView(LocalMessageBase message) 
        : base(message)
    { }

    public override async ValueTask SaveChangesAsync(ButtonEventArgs e)
    {
        await using var scope = Menu.Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        var message = JsonMessage.FromMessage(Message);
        var guild = await db.Guilds.GetOrCreateAsync(e.GuildId!.Value);
        guild.GreetingMessage = message;
        await db.SaveChangesAsync();
        
        await e.Interaction.RespondOrFollowupAsync(new LocalInteractionMessageResponse()
            .WithContent("Greeting message saved.")
            .WithIsEphemeral());
        
        ClearComponents();
        await Menu.ApplyChangesAsync(e);
        Menu.Stop();
    }
}