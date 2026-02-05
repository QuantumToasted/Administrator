using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Extensions.Interactivity.Menus;

namespace Administrator.Bot;

public class GreetingMessageEditView(LocalMessageBase message) : MessageEditView(message)
{
    public override async ValueTask SaveChangesAsync(ButtonEventArgs e)
    {
        await using var scope = Menu.Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        var message = JsonMessage.FromMessage(Message);
        await db.Guilds.Merge(e.GuildId!.Value, _ => new() { GreetingMessage = message });
        
        await e.Interaction.RespondOrFollowupAsync(new LocalInteractionMessageResponse()
            .WithContent("Greeting message saved.")
            .WithIsEphemeral());
        
        ClearComponents();
        await Menu.ApplyChangesAsync(e);
        Menu.Stop();
    }
}