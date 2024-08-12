using Disqord;
using Disqord.Bot;
using Disqord.Rest;
using Humanizer;
using Laylua;
using Qommon;

namespace Administrator.Bot;

public sealed class LuaGuild(IGuild guild, DiscordLuaLibraryBase library) : ILuaModel<LuaGuild>
{
    public long Id { get; } = (long) guild.Id.RawValue;
    
    public string Name { get; } = guild.Name;
    
    public string? Icon { get; } = guild.GetIconUrl(CdnAssetFormat.Automatic, size: 1024);
    
    public string? Splash { get; } = guild.GetSplashUrl(CdnAssetFormat.Automatic, size: 1024);
    
    public string? DiscoverySplash { get; } = guild.GetDiscoverySplashUrl(CdnAssetFormat.Automatic, size: 1024);
    
    public long OwnerId { get; } = (long) guild.OwnerId.RawValue;
    
    //public long? AfkChannelId { get; } = (long?) guild.AfkChannelId?.RawValue;
    
    //public string AfkTimeout { get; } = guild.AfkTimeout.ToString("g");
    
    //public bool IsWidgetEnabled { get; } = guild.IsWidgetEnabled;
    
    //public long? WidgetChannelId { get; } = (long?) guild.WidgetChannelId?.RawValue;
    
    public string VerificationLevel { get; } = guild.VerificationLevel.Humanize(LetterCasing.AllCaps).Replace(' ', '_');
    
    public string NotificationLevel { get; } = guild.NotificationLevel.Humanize(LetterCasing.AllCaps).Replace(' ', '_');
    
    public string ContentFilterLevel { get; } = guild.ContentFilterLevel.Humanize(LetterCasing.AllCaps).Replace(' ', '_');
    
    public LuaRole[] Roles { get; } = guild.Roles.Values.Select(x => new LuaRole(x)).ToArray();
    
    public LuaGuildEmoji[] Emojis { get; } = guild.Emojis.Values.Select(x => new LuaGuildEmoji(x, library)).ToArray();
    
    //public string MfaLevel { get; } = guild.MfaLevel.Humanize(LetterCasing.AllCaps).Replace(' ', '_');
    
    public string[] Features { get; } = guild.Features.ToArray();
    
    //public long? ApplicationId { get; } = (long?) guild.ApplicationId?.RawValue;
    
    public long? SystemChannelId { get; } = (long?) guild.SystemChannelId?.RawValue;
    
    //public long SystemChannelFlags { get; } = (long) guild.SystemChannelFlags;
    
    public long? RulesChannelId { get; } = (long?) guild.RulesChannelId?.RawValue;
    
    //public int? MaxPresenceCount { get; } = guild.MaxPresenceCount;
    
    //public int? MaxMemberCount { get; } = guild.MaxMemberCount;
    
    public string? VanityUrlCode { get; } = guild.VanityUrlCode;
    
    public string? Description { get; } = guild.Description;
    
    public string? Banner { get; } = guild.GetBannerUrl(CdnAssetFormat.Automatic, size: 1024);
    
    public string BoostTier { get; } = guild.BoostTier.Humanize(LetterCasing.AllCaps).Replace(' ', '_');
    
    public int Boosters { get; } = guild.BoostingMemberCount.GetValueOrDefault();
    
    //public string PreferredLocale { get; } = guild.PreferredLocale.Name;
    
    public long? UpdateChannelId { get; } = (long?) guild.PublicUpdatesChannelId?.RawValue;
    
    //public int? MaxVideoMemberCount { get; } = guild.MaxVideoMemberCount;
    
    public string NsfwLevel { get; } = guild.NsfwLevel.Humanize(LetterCasing.AllCaps).Replace(' ', '_');
    
    // TODO: stickers?
    //IReadOnlyDictionary<Snowflake, IGuildSticker> IGuild.Stickers { get; }
    
    //public bool BoostProgressBarEnabled { get; } = guild.IsBoostProgressBarEnabled;
    
    public long? SafetyChannelId { get; } = (long?) guild.SafetyAlertsChannelId?.RawValue;

    public bool BanUser(long id, string? reason, int? pruneDays)
    {
        reason = !string.IsNullOrWhiteSpace(reason) ? reason : "No reason.";
        pruneDays = Math.Max(0, pruneDays.GetValueOrDefault());
        try
        {
            library.RunWait(ct => guild.CreateBanAsync((ulong)id, reason, pruneDays, cancellationToken: ct));
            return true;
        }
        catch
        {
            return false;
        }
    }

    public long SendMessage(long id, LuaTable msg)
    {
        Guard.IsNotNull(msg);
        var message = DiscordLuaLibraryBase.ConvertMessage<LocalMessage>(msg);
        var bot = (DiscordBotBase)guild.Client;
        var newMessage = library.RunWait(ct => bot.SendMessageAsync((ulong) id, message, cancellationToken: ct));
        return (long)newMessage.Id.RawValue;
    }
}