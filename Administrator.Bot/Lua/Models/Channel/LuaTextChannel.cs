using Disqord;
using Disqord.Rest;
using Laylua;
using Qommon;

namespace Administrator.Bot;

public sealed class LuaTextChannel(ITextChannel channel, DiscordLuaLibraryBase library) : LuaGuildChannel(channel), ILuaModel<LuaTextChannel>
{
    public long? LastMessageId { get; } = (long?) channel.LastMessageId?.RawValue;
    
    public string Tag { get; } = channel.Tag;
    
    public string? LastPin { get; } = channel.LastPinTimestamp?.ToString("s");
    
    public bool AgeRestricted { get; } = channel.IsAgeRestricted;
    
    public int Slowmode { get; } = (int) channel.Slowmode.TotalSeconds;
    
    public string? Topic { get; } = channel.Topic;
    
    public long? CategoryId { get; } = (long?) channel.CategoryId?.RawValue;
    
    public bool News { get; } = channel.IsNews;
    
    public string ArchiveThreadsAfter { get; } = channel.DefaultAutomaticArchiveDuration.ToString("g");

    public void SetName(string name)
    {
        Guard.IsNotNullOrWhiteSpace(name);
        library.RunWait(ct => channel.ModifyAsync(x => x.Name = name, cancellationToken: ct));
    }

    public void SetTopic(string topic)
    {
        Guard.IsNotNull(topic);
        library.RunWait(ct => channel.ModifyAsync(x => x.Topic = topic, cancellationToken: ct));
    }
    
    public void Delete()
        => library.RunWait(ct => channel.DeleteAsync(cancellationToken: ct));

    public long SendMessage(LuaTable msg)
    {
        Guard.IsNotNull(msg);
        var message = DiscordLuaLibraryBase.ConvertMessage<LocalMessage>(msg);
        var newMessage = library.RunWait(ct => channel.SendMessageAsync(message, cancellationToken: ct));
        return (long)newMessage.Id.RawValue;
    }

    public LuaMessage? GetMessage(long id)
    {
        var message = library.RunWait(async ct =>
        {
            try
            {
                var m = await channel.GetOrFetchMessageAsync((ulong)id, ct);
                return m;
            }
            catch
            {
                return null;
            }
        });

        return message is not null ? new LuaMessage(message, channel.GuildId, library) : null;
    }
}