using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed class GuildConfiguration : IGuildConfiguration, IEntityTypeConfiguration<GuildConfiguration>
{
    public const string DEFAULT_LEVEL_UP_EMOJI = "🎉";
    
    public Snowflake GuildId { get; init; }

    public GuildSettings Settings { get; set; } = GuildSettings.Default;
    
    public int? MaximumTagsPerUser { get; set; }

    public string LevelUpEmoji { get; set; } = DEFAULT_LEVEL_UP_EMOJI;

    public JsonMessage? GreetingMessage { get; set; }
    
    public bool DmGreetingMessage { get; set; }
    
    public JsonMessage? GoodbyeMessage { get; set; }
    
    public string? CustomPunishmentText { get; set; }
    
    public int? CustomXpRate { get; set; }
    
    public TimeSpan? CustomXpInterval { get; set; }

    public string ApiKey { get; set; } = null!;
    
    public List<Snowflake> XpExemptChannelIds { get; init; } = [];
    
    public List<Snowflake> AutoQuoteExemptChannelIds { get; init; } = [];

    public int DefaultBanPruneDays { get; set; } = 1;

    public int DefaultWarningDemeritPoints { get; set; } = 5;
    
    public TimeSpan? DemeritPointDecayInterval { get; set; } = TimeSpan.FromDays(21);
    
    public Snowflake? JoinRoleId { get; set; }

    public int MaxLuaCommands { get; init; } = 5;
    
    public bool WasVisited { get; set; }
    
    public List<LoggingChannel>? LoggingChannels { get; init; }
    
    public List<ButtonRole>? ButtonRoles { get; init; }
    
    public List<EmojiStats>? EmojiStats { get; init; }
    
    public List<ForumAutoTag>? ForumAutoTags { get; init; }
    
    public List<InviteFilterExemption>? InviteFilterExemptions { get; init; }
    
    public List<LuaCommand>? LuaCommands { get; init; }
    
    public List<RoleLevelReward>? LevelRewards { get; init; }
    
    public List<Tag>? Tags { get; init; }
    
    public List<AutomaticPunishment>? AutomaticPunishments { get; init; }
    
    public List<Punishment>? Punishments { get; init; }
    
    public static GuildConfiguration Create(IGuild guild) => Create(guild.Id);
    public static GuildConfiguration Create(Snowflake guildId)
    {
        return new GuildConfiguration
        {
            GuildId = guildId
        };
    }
    
    IReadOnlyList<Snowflake> IGuildConfiguration.XpExemptChannelIds => XpExemptChannelIds;
    IReadOnlyList<Snowflake> IGuildConfiguration.AutoQuoteExemptChannelIds => AutoQuoteExemptChannelIds;
    IEmoji IGuildConfiguration.LevelUpEmoji => LocalCustomEmoji.TryParse(LevelUpEmoji, out var emoji) ? emoji : new LocalEmoji(LevelUpEmoji);
    void IEntityTypeConfiguration<GuildConfiguration>.Configure(EntityTypeBuilder<GuildConfiguration> config)
    {
        config.HasKey(x => x.GuildId);

        config.Property(x => x.LevelUpEmoji).HasMaxLength(100);
        config.Property(x => x.CustomPunishmentText).HasMaxLength(Discord.Limits.Message.Embed.Field.MaxValueLength);
        config.Property(x => x.ApiKey).HasMaxLength(40);
        config.Property(x => x.GreetingMessage).HasColumnType("jsonb");
        config.Property(x => x.GoodbyeMessage).HasColumnType("jsonb");
        config.Property(x => x.ApiKey).HasDefaultValueSql("REPLACE(gen_random_uuid()::text, '-', '' )");

        config.HasMany(x => x.LoggingChannels).WithOne(x => x.Guild).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.NoAction);
        config.HasMany(x => x.ButtonRoles).WithOne(x => x.Guild).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.NoAction);
        config.HasMany(x => x.EmojiStats).WithOne(x => x.Guild).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.NoAction);
        config.HasMany(x => x.ForumAutoTags).WithOne(x => x.Guild).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.NoAction);
        config.HasMany(x => x.InviteFilterExemptions).WithOne(x => x.Guild).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.NoAction);
        config.HasMany(x => x.LuaCommands).WithOne(x => x.Guild).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.NoAction);
        config.HasMany(x => x.LevelRewards).WithOne(x => x.Guild).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.NoAction);
        config.HasMany(x => x.Tags).WithOne(x => x.Guild).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.NoAction);
        config.HasMany(x => x.AutomaticPunishments).WithOne(x => x.Guild).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.NoAction);
        config.HasMany(x => x.Punishments).WithOne(x => x.Guild).HasForeignKey(x => x.GuildId).OnDelete(DeleteBehavior.NoAction);
    }
}