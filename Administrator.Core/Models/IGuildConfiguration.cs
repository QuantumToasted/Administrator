using System.ComponentModel;
using Disqord;

namespace Administrator.Core;

[Flags]
public enum GuildSettings
{
    [Description("Whether non-bot bans, timeouts, etc. generate punishment cases.")]
    AutomaticPunishmentDetection = 1 << 0,
    [Description("Whether moderators' names will be shown in punishment embeds.")]
    LogModeratorsInPunishments = 1 << 1,
    [Description("Whether image attachments will be shown in punishment embeds.")]
    LogImagesInPunishments = 1 << 2,
    [Description("Whether Discord invites not from this server will be filtered when posted.")]
    FilterDiscordInvites = 1 << 3,
    [Description("Whether server XP will be tracked and incremented over time.")]
    TrackServerXp = 1 << 4,
    [Description("Whether bot message updates/deletes will be logged (requires channel setup).")]
    IgnoreBotMessages = 1 << 5,
    [Description("AutoQuote: Whether message links posted will trigger an automatic quote post by the bot.")]
    AutoQuote = 1 << 6,
    Default = TrackServerXp | AutomaticPunishmentDetection | LogModeratorsInPunishments | IgnoreBotMessages | AutoQuote
}

public interface IGuildConfiguration : IGuildEntity
{
    GuildSettings Settings { get; }
    
    int? MaximumTagsPerUser { get; }
    
    IEmoji LevelUpEmoji { get; }
    
    JsonMessage? GreetingMessage { get; }
    
    bool DmGreetingMessage { get; }
    
    JsonMessage? GoodbyeMessage { get; }
    
    string? CustomPunishmentText { get; }
    
    int? CustomXpRate { get; }
    
    TimeSpan? CustomXpInterval { get; }
    
    string ApiKey { get; }
    
    IReadOnlyList<Snowflake> XpExemptChannelIds { get; }
    
    IReadOnlyList<Snowflake> AutoQuoteExemptChannelIds { get; }
    
    int DefaultBanPruneDays { get; }
    
    int DefaultWarningDemeritPoints { get; }
    
    TimeSpan? DemeritPointDecayInterval { get; }
    
    Snowflake? JoinRoleId { get; }
    
    int MaxLuaCommands { get; }
}