using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Rest;

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

        await e.Interaction.Followup().SendAsync(new LocalInteractionMessageResponse()
            .WithContent("Changes saved.")
            .WithIsEphemeral());
        
        ChangesSaved = true;
        ClearComponents();
        await Menu.ApplyChangesAsync(e);
        Menu.Stop();
    }
}