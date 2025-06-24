using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Extensions.Interactivity.Menus;

namespace Administrator.Bot;

public class GoodbyeMessageEditView(LocalMessageBase message) : MessageEditView(message)
{
    public override async ValueTask SaveChangesAsync(ButtonEventArgs e)
    {
        await using var scope = Menu.Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        var message = JsonMessage.FromMessage(Message);
        await db.Guilds.Merge(e.GuildId!.Value, _ => new() { GoodbyeMessage = message });
        
        await e.Interaction.RespondOrFollowupAsync(new LocalInteractionMessageResponse()
            .WithContent("Goodbye message saved.")
            .WithIsEphemeral());
        
        ClearComponents();
        await Menu.ApplyChangesAsync(e);
        Menu.Stop();
    }
}