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
    AutomaticPunishmentDetection = 1 << 0,
    LogModeratorsInPunishments = 1 << 1,
    LogImagesInPunishments = 1 << 2,
    FilterDiscordInvites = 1 << 3,
    TrackServerXp = 1 << 4,
    IgnoreBotMessages = 1 << 5,
    AutoQuote = 1 << 6
}

public sealed record Guild(Snowflake GuildId)
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

    public List<Snowflake> XpExemptChannelIds { get; set; } = new();

    public List<Snowflake> AutoQuoteExemptChannelIds { get; set; } = new();

    public int DefaultBanPruneDays { get; set; } = 1;

    public int DefaultWarningDemeritPoints { get; set; } = 5;

    public TimeSpan? DemeritPointsDecayInterval { get; set; } = TimeSpan.FromDays(21); // 3 weeks
    
    public Snowflake? JoinRoleId { get; set; }

    public int MaxLuaCommands { get; set; } = 5;
    
#pragma warning disable CS8618
    
    public List<LoggingChannel> LoggingChannels { get; init; }
    
    public List<ButtonRole> ButtonRoles { get; init; }
    
    public List<EmojiStats> EmojiStats { get; init; }
    
    public List<ForumAutoTag> ForumAutoTags { get; init; }
    
    public List<InviteFilterExemption> InviteFilterExemptions { get; init; }
    
    public List<LuaCommand> LuaCommands { get; init; }
    
    public List<RoleLevelReward> LevelRewards { get; init; }
    
    public List<Tag> Tags { get; init; }
    
    public List<AutomaticPunishment> AutomaticPunishments { get; init; }
    
    public List<Punishment> Punishments { get; init; }
#pragma warning restore CS8618

    private sealed class GuildConfiguration : IEntityTypeConfiguration<Guild>
    {
        public void Configure(EntityTypeBuilder<Guild> guild)
        {
            guild.HasKey(x => x.GuildId);

            guild.Property(x => x.GreetingMessage).HasColumnType("jsonb");
            guild.Property(x => x.GoodbyeMessage).HasColumnType("jsonb");

            guild.HasMany(x => x.LoggingChannels).WithOne(x => x.Guild).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.NoAction);
            guild.HasMany(x => x.ButtonRoles).WithOne(x => x.Guild).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.NoAction);
            guild.HasMany(x => x.EmojiStats).WithOne(x => x.Guild).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.NoAction);
            guild.HasMany(x => x.ForumAutoTags).WithOne(x => x.Guild).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.NoAction);
            guild.HasMany(x => x.InviteFilterExemptions).WithOne(x => x.Guild).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.NoAction);
            guild.HasMany(x => x.LuaCommands).WithOne(x => x.Guild).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.NoAction);
            guild.HasMany(x => x.LevelRewards).WithOne(x => x.Guild).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.NoAction);
            guild.HasMany(x => x.Tags).WithOne(x => x.Guild).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.NoAction);
            guild.HasMany(x => x.AutomaticPunishments).WithOne(x => x.Guild).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.NoAction);
            guild.HasMany(x => x.Punishments).WithOne(x => x.Guild).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}