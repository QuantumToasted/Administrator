using Administrator.Core;
using Disqord;
using Disqord.Extensions.Interactivity.Menus;

namespace Administrator.Bot;

public sealed class TagMessageEditView(string tagName, LocalMessageBase message) 
    : MessageEditView(message)
{
    public override async ValueTask SaveChangesAsync(ButtonEventArgs e)
    {
        await using var scope = Menu.Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        var dbTag = await db.Tags.FindAsync(e.GuildId!.Value, tagName);

        dbTag!.Message = JsonMessage.FromMessage(Message);
        await db.SaveChangesAsync();

        await e.Interaction.RespondOrFollowupAsync(new LocalInteractionMessageResponse()
            .WithContent("Changes saved.")
            .WithIsEphemeral());
        
        ClearComponents();
        await Menu.ApplyChangesAsync(e);
        Menu.Stop();
    }
}