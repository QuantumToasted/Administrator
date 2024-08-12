using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands;
using Disqord.Extensions.Interactivity.Menus;
using Microsoft.EntityFrameworkCore;

namespace Administrator.Bot;

public sealed class TagLinkView : AdminViewBase
{
    private readonly IDiscordGuildCommandContext _context;
    private readonly HashSet<string> _shownNonEphemeralTags = new();
    
    public TagLinkView(IDiscordGuildCommandContext context, LocalMessageBase message, IEnumerable<TagLink> links, bool isEphemeral) : base(null)
    {
        _context = context;
        
        MessageTemplate = x =>
        {
            x.Content = message.Content;
            x.Attachments = message.Attachments;
            x.Embeds = message.Embeds;
            (x as LocalInteractionMessageResponse)?.WithIsEphemeral(isEphemeral);
        };

        foreach (var link in links)
        {
            AddComponent(new ButtonViewComponent(e => ShowTagAsync(e, link.To, link.IsEphemeral))
            {
                Label = link.Label ?? link.To,
                Style = link.Style
            });
        }
    }

    private async ValueTask ShowTagAsync(ButtonEventArgs e, string name, bool isEphemeral)
    {
        if (!isEphemeral && !_shownNonEphemeralTags.Add(name))
        {
            await e.Interaction.RespondOrFollowupAsync(new LocalInteractionMessageResponse()
                .WithContent($"The linked tag {Markdown.Bold(name)} has already been sent recently.")
                .WithIsEphemeral());
            
            return;
        }
        
        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);

        if (await db.Tags.FirstOrDefaultAsync(x => x.GuildId == _context.GuildId && x.Name == name) is not { } tag)
        {
            await e.Interaction.RespondOrFollowupAsync(new LocalInteractionMessageResponse()
                .WithContent($"The linked tag {Markdown.Bold(name)} has been deleted...sorry!")
                .WithIsEphemeral());
            return;
        }
        
        await db.Tags.Entry(tag).Reference(x => x.Attachment).LoadAsync();
        await tag.ShowAsync(_context, e.Interaction, isEphemeral);
        /*

        tag.Uses++;
        tag.LastUsedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        await e.Interaction.Response().DeferAsync(isEphemeral);
        var message = await tag.ToLocalMessageAsync<LocalInteractionMessageResponse>(_context);
        message.WithIsEphemeral(isEphemeral);
        await e.Interaction.RespondOrFollowupAsync(message);
        */
    }
}