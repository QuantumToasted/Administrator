using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Disqord.Bot.Commands.Interaction;
using Disqord.Gateway;
using Microsoft.EntityFrameworkCore;

namespace Administrator.Bot;

// TODO: maybe remove this or extrapolate into a util method. I don't like the idea of coupling modules to services.
[ScopedService]
public sealed class HighlightService(AdminDbContext db, ICommandContextAccessor contextAccessor, AutoCompleteService autoComplete, 
    HighlightHandlingService highlights)
{
    private readonly IDiscordInteractionCommandContext _context = (IDiscordInteractionCommandContext)contextAccessor.Context;

    public async Task<Result<Highlight>> CreateHighlightAsync(string text)
    {
        var guild = _context.GuildId.HasValue
            ? _context.Bot.GetGuild(_context.GuildId.Value)
            : null;

        if (await db.Highlights
                .FirstOrDefaultAsync(x => x.AuthorId == _context.AuthorId && x.GuildId == _context.GuildId && x.Text.Equals(text)) is not null)
        {
            return guild is not null
                ? $"You already have a highlight in {Markdown.Bold(guild.Name)} for the text \"{text}\"!"
                : $"You already have a global highlight for the text \"{text}\"!";
        }

        var highlight = new Highlight(_context.AuthorId, _context.GuildId, text);
        
        db.Highlights.Add(highlight);
        await db.SaveChangesAsync();
        highlights.InvalidateCache();

        return highlight;
    }

    public async Task<Result<Highlight>> RemoveHighlightAsync(int id)
    {
        if (await db.Highlights.FirstOrDefaultAsync(x => x.AuthorId == _context.AuthorId && x.Id == id) is not { } highlight)
            return $"You don't have a highlight with the ID {Markdown.Bold(id)}.";

        db.Highlights.Remove(highlight);
        await db.SaveChangesAsync();
        highlights.InvalidateCache();

        return highlight;
    }

    public async Task AutoCompleteHighlightsAsync(AutoComplete<int> id)
    {
        var highlights = await db.Highlights.Where(x => x.AuthorId == _context.AuthorId)
            .OrderByDescending(x => x.Id)
            .ToListAsync();
        
        autoComplete.AutoComplete(id, highlights);
    }
}