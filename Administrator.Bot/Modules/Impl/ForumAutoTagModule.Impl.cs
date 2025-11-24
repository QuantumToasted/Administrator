using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Gateway;
using LinqToDB;
using Qmmands;

namespace Administrator.Bot;

public sealed partial class ForumAutoTagModule(AdminDbContext db) : DiscordApplicationGuildModuleBase
{
    public partial async Task<IResult> Add(IChannel channel, string text, string tag, bool isRegex)
    {
        var forum = Bot.GetChannel(Context.GuildId, channel.Id) as IForumChannel;
        if (forum?.Tags.FirstOrDefault(x => x.Id.ToString() == tag || x.Name == tag) is not { } forumTag)
            return Response($"No valid tag could be found in the forum {Mention.Channel(channel.Id)} with the input \"{tag}\".").AsEphemeral();

        if (await db.AutoTags.FirstOrDefaultAsync(x => x.Text == text && x.ChannelId == channel.Id && x.GuildId == Context.GuildId) is { } existingAutoTag)
            return Response($"Existing automatic tag {existingAutoTag} already matches the same text and forum channel!").AsEphemeral();
        
        ForumAutoTag autoTag;
        if (isRegex)
        {
            if (!RegexUtil.TryCreate(text, out var regex))
                return Response($"Invalid regex. Try using a tool like {RegexUtil.REGEX_HELPER_SITE} to design your regex!").AsEphemeral();

            autoTag = ForumAutoTag.FromRegex(forum, regex, forumTag);
        }
        else
        {
            autoTag = ForumAutoTag.FromText(forum, text, forumTag);
        }

        db.AutoTags.Add(autoTag);
        await db.SaveChangesAsync();

        return Response($"New forum auto tag {autoTag} created.\n" +
                        $"When users make a post in {Mention.Channel(channel.Id)} matching {Markdown.Code(text)}, the tag " +
                        $"{forumTag.Name} will be automatically applied.");
    }

    public partial async Task<IResult> Remove(int autoTagId)
    {
        if (await db.AutoTags.FirstOrDefaultAsync(x => x.Id == autoTagId && x.GuildId == Context.GuildId) is not { } autoTag)
            return Response("No automatic tag could be found with that ID.").AsEphemeral();

        db.AutoTags.Remove(autoTag);
        await db.SaveChangesAsync();

        return Response($"Automatic tag {autoTag} removed.");
    }

    public partial void AutoCompleteForumTags(AutoComplete<string> tag)
    {
        if (!tag.IsFocused)
            return;
        
        var interaction = (IAutoCompleteInteraction)Context.Interaction;
        var raw = interaction.Options["add"].Options["channel"].Value?.ToString();

        if (!Snowflake.TryParse(raw, out var channelId) || Bot.GetChannel(Context.GuildId, channelId) is not IForumChannel { Tags: var tags })
            return;

        tag.AutoComplete(Context, tags.ToList());
    }

    public partial async Task AutoCompleteForumAutoTags(AutoComplete<int> autoTagId)
    {
        if (!autoTagId.IsFocused)
            return;

        var autoTags = await db.AutoTags.Where(x => x.GuildId == Context.GuildId).ToListAsync();
        autoTagId.AutoComplete(Context, autoTags);
    }
}