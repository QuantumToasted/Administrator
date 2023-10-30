using System.Text;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Disqord.Rest;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Administrator.Bot;

public sealed class ForumAutoTagService : DiscordBotService
{
    protected override async ValueTask OnThreadCreated(ThreadCreatedEventArgs e)
    {
        if (!e.IsThreadCreation || e.Thread.GetChannel() is not IForumChannel { Tags: { } forumTags })
            return;

        var openingMessage = await e.Thread.GetOrFetchMessageAsync(e.Thread.LastMessageId!.Value);
        if (string.IsNullOrWhiteSpace(openingMessage?.Content))
            return;
        
        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        var autoTags = await db.AutoTags.Where(x => x.ChannelId == e.Thread.ChannelId).ToListAsync();

        if (autoTags.Count == 0)
            return;

        var tagsToAdd = new List<IForumTag>();
        foreach (var autoTag in autoTags)
        {
            // remove stale tags
            if (forumTags.FirstOrDefault(x => x.Id == autoTag.TagId) is not { } forumTag)
            {
                Logger.LogDebug("Removing auto-tag {TagId} as its tag was deleted.", autoTag.TagId.RawValue);
                db.AutoTags.Remove(autoTag);
                continue;
            }

            if (!e.Thread.TagIds.Contains(autoTag.TagId) && autoTag.IsMatch(openingMessage))
                tagsToAdd.Add(forumTag);
        }
        
        // just in case stale tags were removed, save
        await db.SaveChangesAsync();

        if (tagsToAdd.Count == 0)
            return;

        var contentBuilder = new StringBuilder()
            .AppendNewline("This post has automatically been tagged with the following tags based on its content:")
            .AppendJoin(", ", tagsToAdd.Select(x => $"{x.Emoji} {Markdown.Bold(x.Name)}"));

        try
        {
            await e.Thread.ModifyAsync(x => x.TagIds = e.Thread.TagIds.Concat(tagsToAdd.Select(y => y.Id)).Distinct().ToList());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to auto-tag post {PostId} with tags {TagIds}", e.ThreadId.RawValue,
                tagsToAdd.Select(x => x.Id.RawValue).ToList());

            contentBuilder.AppendNewline()
                .AppendNewline("However, due to missing permissions or another error, tags were not able to be added.");
        }

        await e.Thread.SendMessageAsync(new LocalMessage()
            .WithContent(contentBuilder.ToString()));
    }
}