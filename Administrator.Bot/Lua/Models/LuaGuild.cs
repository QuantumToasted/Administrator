using Disqord;
using Humanizer;

namespace Administrator.Bot;

public sealed class LuaGuild(IGuild guild)
{
    public Snowflake Id { get; } = guild.Id;
    
    public string Name { get; } = guild.Name;
    
    public string? Icon { get; } = guild.GetIconUrl(CdnAssetFormat.Automatic, size: 512);
    
    public string? Splash { get; } = guild.GetSplashUrl(CdnAssetFormat.Automatic, size: 512);
    
    public string? DiscoverySplash { get; } = guild.GetDiscoverySplashUrl(CdnAssetFormat.Automatic, size: 512);
    
    public Snowflake OwnerId { get; } = guild.OwnerId;
    
    public string VerificationLevel { get; } = guild.VerificationLevel.Humanize(LetterCasing.AllCaps).Replace(' ', '_');
    
    public string NotificationLevel { get; } = guild.NotificationLevel.Humanize(LetterCasing.AllCaps).Replace(' ', '_');
    
    public string ContentFilterLevel { get; } = guild.ContentFilterLevel.Humanize(LetterCasing.AllCaps).Replace(' ', '_');
    
    public LuaRole[] Roles { get; } = guild.Roles.Values.Select(x => new LuaRole(x)).ToArray();
    
    public LuaGuildEmoji[] Emojis { get; } = guild.Emojis.Values.Select(x => new LuaGuildEmoji(x)).ToArray();
    
    public string[] Features { get; } = guild.Features.ToArray();
    
    public Snowflake? SystemChannelId { get; } = guild.SystemChannelId;

    public Snowflake? RulesChannelId { get; } = guild.RulesChannelId;
    
    public string? VanityUrlCode { get; } = guild.VanityUrlCode;
    
    public string? Description { get; } = guild.Description;
    
    public string? Banner { get; } = guild.GetBannerUrl(CdnAssetFormat.Automatic, size: 512);
    
    public string BoostTier { get; } = guild.BoostTier.Humanize(LetterCasing.AllCaps).Replace(' ', '_');
    
    public int Boosters { get; } = guild.BoostingMemberCount.GetValueOrDefault();
    
    public Snowflake? UpdateChannelId { get; } = guild.PublicUpdatesChannelId;
    
    public string NsfwLevel { get; } = guild.NsfwLevel.Humanize(LetterCasing.AllCaps).Replace(' ', '_');
    
    // TODO: stickers?
    //IReadOnlyDictionary<Snowflake, IGuildSticker> IGuild.Stickers { get; }

    public Snowflake? SafetyChannelId { get; } = guild.SafetyAlertsChannelId;
}