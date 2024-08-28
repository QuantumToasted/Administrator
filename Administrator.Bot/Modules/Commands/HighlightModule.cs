using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Qmmands;

namespace Administrator.Bot;

[SlashGroup("highlight")]
public sealed partial class HighlightModule
{
    [SlashCommand("clear")]
    [Description("Clears all your highlights for this server. If in DMs, clears all global highlights.")]
    public partial Task Clear();

    [SlashCommand("list")]
    [Description("Lists all your highlights for this server. If in DMs, lists all your global highlights.")]
    public partial Task<IResult> List();

    [SlashCommand("create")]
    [Description("Creates a new highlight for a server. If in DMs, adds a new global highlight instead.")]
    public partial Task<IResult> Add(
        [Description("The text you wish to be highlighted for.")]
        [Maximum(25)]
            string text);

    [SlashCommand("remove")]
    [Description("Deletes one of your highlights.")]
    public partial Task<IResult> Delete(
        [Description("The ID of the highlight to delete.")]
            int id);

    [AutoComplete("remove")]
    public partial Task AutoCompleteHighlights(AutoComplete<int> id);

    [SlashGroup("blacklist")]
    public sealed partial class HighlightBlacklistModule
    {
        [SlashCommand("view")]
        [Description("Lists your current highlight blacklist (channels or users).")]
        public partial Task<IResult> View(ViewMode mode);

        [SlashCommand("add")]
        [Description("Adds a user or channel to your highlight blacklist.")]
        public partial Task<IResult> Add(
            [Description("The user to add to your highlight blacklist.")]
            [RequireNotAuthor]
                IUser? user = null,
            [Description("The channel to add to your highlight blacklist.")]
            [ChannelTypes(ChannelType.Text, ChannelType.PrivateThread, ChannelType.PublicThread)]
            [AuthorCanViewChannel]
                IChannel? channel = null);

        [SlashCommand("remove")]
        [Description("Removes a user or channel from your highlight blacklist.")]
        public partial Task<IResult> Remove(
            [Description("The user to remove from your highlight blacklist.")]
                IUser? user = null,
            [Description("The channel to remove from your highlight blacklist.")]
            [ChannelTypes(ChannelType.Text, ChannelType.PrivateThread, ChannelType.PublicThread)]
            [AuthorCanViewChannel]
                IChannel? channel = null);
    }

    [SlashGroup("snooze")]
    public sealed partial class HighlightSnoozeModule
    {
        [SlashCommand("until")]
        [Description("Snoozes all highlights until this date/time.")]
        public partial Task<IResult> Until(
            [Description("A duration (2h30m) or instant in time (tomorrow at noon).")]
                DateTimeOffset time);

        [SlashCommand("cancel")]
        [Description("Cancels any current highlight snoozing.")]
        public partial Task<IResult> Cancel();
    }
}