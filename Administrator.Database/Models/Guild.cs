using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

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

public enum GuildSettingFlags
{
    //[ChoiceName("AutomaticPunishmentDetection: Whether non-bot bans, timeouts, etc. generate punishment cases.")]
    AutomaticPunishmentDetection = 1 << 0,
    //[ChoiceName("LogModeratorsInPunishments: Whether moderators' names will be shown in punishment embeds.")]
    LogModeratorsInPunishments = 1 << 1,
    //[ChoiceName("LogImagesInPunishments: Whether image attachments will be shown in punishment embeds.")]
    LogImagesInPunishments = 1 << 2,
    //[ChoiceName("FilterDiscordInvites: Whether Discord invites not from this server will be filtered when posted.")]
    FilterDiscordInvites = 1 << 3,
    //[ChoiceName("TrackServerXp: Whether server XP will be tracked and incremented over time.")]
    TrackServerXp = 1 << 4,
    //[ChoiceName("IgnoreBotMessages: Whether bot message updates/deletes will be logged (requires channel setup).")]
    IgnoreBotMessages = 1 << 5,
    //[ChoiceName("AutoQuote: Whether message links posted will trigger an automatic quote post by the bot.")]
    AutoQuote = 1 << 6
}

[Table("guilds")]
[PrimaryKey(nameof(Id))]
public sealed record Guild(
    [property: Column("id")] ulong Id)
{
    public const string DEFAULT_LEVEL_UP_EMOJI = "🎉";
    
    [Column("settings")]
    public GuildSettings Settings { get; set; } = GuildSettings.Default;
    
    [Column("max_tags")]
    public int? MaximumTagsPerUser { get; set; }

    [Column("level_up_emoji")]
    public string LevelUpEmoji { get; set; } = DEFAULT_LEVEL_UP_EMOJI;
    
    [Column("greeting", TypeName = "jsonb")]
    public JsonMessage? GreetingMessage { get; set; }
    
    [Column("dm_greeting")]
    public bool DmGreetingMessage { get; set; }
    
    [Column("goodbye", TypeName = "jsonb")]
    public JsonMessage? GoodbyeMessage { get; set; }
    
    [Column("was_visited")]
    public bool WasVisited { get; set; }
    
    [Column("punishment_text")]
    public string? CustomPunishmentText { get; set; }
    
    [Column("xp_rate")]
    public int? CustomXpRate { get; set; }
    
    [Column("xp_interval")]
    public TimeSpan? CustomXpInterval { get; set; }
    
    [Column("api_salt")]
    public byte[]? ApiKeySalt { get; set; }
    
    [Column("api_hash")]
    public byte[]? ApiKeyHash { get; set; }
    
    [Column("xp_exempt_channels")]
    public HashSet<ulong> XpExemptChannelIds { get; set; } = new();

    [Column("auto_quote_exempt_channels")]
    public HashSet<ulong> AutoQuoteExemptChannelIds { get; set; } = new();
    
    public List<LoggingChannel> LoggingChannels { get; init; }
    
    public List<ButtonRole> ButtonRoles { get; init; }
    
    public List<EmojiStats> EmojiStats { get; init; }
    
    public List<ForumAutoTag> ForumAutoTags { get; init; }
    
    public List<InviteFilterExemption> InviteFilterExemptions { get; init; }
    
    public List<LuaCommand> LuaCommands { get; init; }
    
    public List<RoleLevelReward> LevelRewards { get; init; }
    
    public List<Tag> Tags { get; init; }
    
    public List<WarningPunishment> WarningPunishments { get; init; }
}