using Disqord;
using Disqord.Models;
using Laylua.Marshaling;
using Qommon;

namespace Administrator.Bot;

[LuaType]
public sealed partial class LuaDiscordMessage(IMessage message) : LuaMessage, IMessage
{
    public Snowflake Id { get; } = message.Id;

    [LuaName("channel")] 
    public Snowflake ChannelId { get; } = message.ChannelId;

    public LuaUser Author { get; } = LuaUser.FromUser(message.Author);
    
    string IMessage.Content => Content ?? string.Empty;
    IClient IClientEntity.Client => null!;
    void IJsonUpdatable<MessageJsonModel>.Update(MessageJsonModel model) => throw new NotSupportedException();
    IUser IMessage.Author => Author;
    IReadOnlyList<IUser> IMessage.MentionedUsers => [];
    Optional<IReadOnlyDictionary<IEmoji, IMessageReaction>> IMessage.Reactions => Optional<IReadOnlyDictionary<IEmoji, IMessageReaction>>.Empty;
    MessageFlags IMessage.Flags => MessageFlags.None;
}