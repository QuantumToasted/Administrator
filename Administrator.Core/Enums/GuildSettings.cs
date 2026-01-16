using System.ComponentModel;

namespace Administrator.Core;

[Flags]
public enum GuildSettings
{
    [IgnoreEnumValue] None = 0,
    [Description("Whether the audit log will be inspected for relevant punishment cases to generate.")]
    AutomaticPunishmentDetection = 1 << 0,
    [Description("Whether moderator information will be logged in punishment case log messages.")]
    LogModeratorsInPunishments = 1 << 1,
    [Description("Whether images/attachments will be logged in punishment case log messages.")]
    LogImagesInPunishments = 1 << 2,
    [Description("Whether invites to servers other than this one will be filtered automatically.")]
    FilterDiscordInvites = 1 << 3,
    [Description("Whether server XP will be tracked via message activity.")]
    TrackServerXp = 1 << 4,
    [Description("Whether to ignore messages from bots being deleted or updated when logging.")]
    IgnoreBotMessages = 1 << 5,
    [Description("Whether to automatically display a quote in chat when a message link is posted.")]
    AutoQuote = 1 << 6,
    [Description("Whether to add reactions detailing a user's XP level increasing.")]
    LevelUpReactions = 1 << 7,
    [IgnoreEnumValue] Default = TrackServerXp | AutomaticPunishmentDetection | LogModeratorsInPunishments | IgnoreBotMessages | AutoQuote | LevelUpReactions
}