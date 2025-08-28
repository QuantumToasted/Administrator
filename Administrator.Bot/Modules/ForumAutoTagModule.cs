using Disqord;
using Disqord.Bot.Commands.Application;
using Qmmands;

namespace Administrator.Bot;

[SlashGroup("forum-auto-tag")]
public sealed partial class ForumAutoTagModule
{
    [SlashCommand("add")]
    [Description("Adds a new automatic tag to a forum channel.")]
    public partial Task<IResult> Add(
        [Description("The forum channel to add an automatic tag for.")]
        [ChannelTypes(ChannelType.Forum)]
        IChannel channel,
        [Description("Messages containing this text will trigger the match.")]
        [Range(2, 100)]
        string text,
        [Description("The ID or name of the tag to match.")]
        string tag,
        [Description("Whether to interpret 'text' as a regular expression (regex) instead.")]
        bool isRegex = false);

    [SlashCommand("remove")]
    [Description("Removes an existing automatic tag.")]
    public partial Task<IResult> Remove(
        [Name("auto-tag")]
        [Description("The tag to remove.")]
            int autoTagId);

    [AutoComplete("add")]
    public partial void AutoCompleteForumTags(AutoComplete<string> tag);

    [AutoComplete("remove")]
    public partial Task AutoCompleteForumAutoTags([Name("auto-tag")] AutoComplete<int> autoTagId);
}