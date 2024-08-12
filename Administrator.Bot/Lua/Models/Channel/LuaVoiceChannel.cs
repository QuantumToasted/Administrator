using Disqord;
using Disqord.Rest;
using Humanizer;
using Laylua;

namespace Administrator.Bot;

public sealed class LuaVoiceChannel(IVoiceChannel channel, DiscordLuaLibraryBase library) : LuaGuildChannel(channel), ILuaModel<LuaVoiceChannel>
{
    public long? CategoryId { get; } = (long?) channel.CategoryId?.RawValue;

    //public int Bitrate { get; } = channel.Bitrate;

    public string? Region { get; } = channel.Region;

    public long? LastMessageId { get; } = (long?) channel.LastMessageId?.RawValue;

    public string? LastPinTimestamp { get; } = channel.LastPinTimestamp?.ToString("s");

    public string Slowmode { get; } = channel.Slowmode.ToString("s");

    public string Tag { get; } = channel.Tag;

    public bool IsAgeRestricted { get; } = channel.IsAgeRestricted;

    public int MemberLimit { get; } = channel.MemberLimit;

    //public string VideoQualityMode { get; } = channel.VideoQualityMode.Humanize(LetterCasing.AllCaps).Replace(' ', '_');

    public void SetName(string name)
        => library.RunWait(ct => channel.ModifyAsync(x => x.Name = name, cancellationToken: ct));

    public void Delete()
        => library.RunWait(ct => channel.DeleteAsync(cancellationToken: ct));
    
    public long SendMessage(LuaTable msg)
    {
        var message = DiscordLuaLibraryBase.ConvertMessage<LocalMessage>(msg);
        var newMessage = library.RunWait(ct => channel.SendMessageAsync(message, cancellationToken: ct));
        return (long)newMessage.Id.RawValue;
    }
}