using System.ComponentModel;
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

public sealed record Guild(Snowflake GuildId) : IStaticEntityTypeConfiguration<Guild>
{
    public const string DEFAULT_LEVEL_UP_EMOJI = "🎉";
    
    public GuildSettings Settings { get; set; } = GuildSettings.Default;
    
    public int? MaximumTagsPerUser { get; set; }

    public string LevelUpEmoji { get; set; } = DEFAULT_LEVEL_UP_EMOJI;
    
    public JsonMessage? GreetingMessage { get; set; }
    
    public bool DmGreetingMessage { get; set; }
    
    public JsonMessage? GoodbyeMessage { get; set; }
    
    public bool WasVisited { get; set; }
    
    public string? CustomPunishmentText { get; set; }
    
    public int? CustomXpRate { get; set; }
    
    public TimeSpan? CustomXpInterval { get; set; }
    
    public byte[]? ApiKeySalt { get; set; }
    
    public byte[]? ApiKeyHash { get; set; }
    
    public HashSet<Snowflake> XpExemptChannelIds { get; set; } = new();

    public HashSet<Snowflake> AutoQuoteExemptChannelIds { get; set; } = new();

    public int DefaultBanPruneDays { get; set; } = 1;
    
#pragma warning disable CS8618
    public List<GuildUser> Users { get; init; }
    
    public List<LoggingChannel> LoggingChannels { get; init; }
    
    public List<ButtonRole> ButtonRoles { get; init; }
    
    public List<EmojiStats> EmojiStats { get; init; }
    
    public List<ForumAutoTag> ForumAutoTags { get; init; }
    
    public List<InviteFilterExemption> InviteFilterExemptions { get; init; }
    
    public List<LuaCommand> LuaCommands { get; init; }
    
    public List<RoleLevelReward> LevelRewards { get; init; }
    
    public List<Tag> Tags { get; init; }
    
    public List<WarningPunishment> WarningPunishments { get; init; }
    
    public List<Punishment> Punishments { get; init; }
#pragma warning restore CS8618
    
    static void IStaticEntityTypeConfiguration<Guild>.ConfigureBuilder(EntityTypeBuilder<Guild> guild)
    {
        guild.ToTable("guilds");
        guild.HasKey(x => x.GuildId);

        guild.HasPropertyWithColumnName(x => x.GuildId, "guild");
        guild.HasPropertyWithColumnName(x => x.Settings, "settings");
        guild.HasPropertyWithColumnName(x => x.MaximumTagsPerUser, "max_tags_per_users");
        guild.HasPropertyWithColumnName(x => x.LevelUpEmoji, "level_up_emoji");
        guild.HasPropertyWithColumnName(x => x.GreetingMessage, "greeting").HasColumnType("jsonb");
        guild.HasPropertyWithColumnName(x => x.DmGreetingMessage, "dm_greeting");
        guild.HasPropertyWithColumnName(x => x.GoodbyeMessage, "goodbye").HasColumnType("jsonb");
        guild.HasPropertyWithColumnName(x => x.WasVisited, "was_visited");
        guild.HasPropertyWithColumnName(x => x.CustomPunishmentText, "punishment_text");
        guild.HasPropertyWithColumnName(x => x.CustomXpRate, "xp_rate");
        guild.HasPropertyWithColumnName(x => x.CustomXpInterval, "xp_interval");
        guild.HasPropertyWithColumnName(x => x.ApiKeySalt, "api_salt");
        guild.HasPropertyWithColumnName(x => x.ApiKeyHash, "api_hash");
        guild.HasPropertyWithColumnName(x => x.XpExemptChannelIds, "xp_exempt_channels");
        guild.HasPropertyWithColumnName(x => x.AutoQuoteExemptChannelIds, "autoquote_exempt_channels");
        guild.HasPropertyWithColumnName(x => x.DefaultBanPruneDays, "ban_prune_days");

        guild.HasMany(x => x.Users).WithOne(x => x.Guild).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.Cascade);
        guild.HasMany(x => x.LoggingChannels).WithOne(x => x.Guild).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.Cascade);
        guild.HasMany(x => x.ButtonRoles).WithOne(x => x.Guild).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.Cascade);
        guild.HasMany(x => x.EmojiStats).WithOne(x => x.Guild).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.Cascade);
        guild.HasMany(x => x.ForumAutoTags).WithOne(x => x.Guild).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.Cascade);
        guild.HasMany(x => x.InviteFilterExemptions).WithOne(x => x.Guild).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.Cascade);
        guild.HasMany(x => x.LuaCommands).WithOne(x => x.Guild).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.Cascade);
        guild.HasMany(x => x.LevelRewards).WithOne(x => x.Guild).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.Cascade);
        guild.HasMany(x => x.Tags).WithOne(x => x.Guild).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.Cascade);
        guild.HasMany(x => x.WarningPunishments).WithOne(x => x.Guild).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.Cascade);
        guild.HasMany(x => x.Punishments).WithOne(x => x.Guild).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.Cascade);
    }
}