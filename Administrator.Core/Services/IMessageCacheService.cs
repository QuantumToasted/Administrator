using Disqord;

using MessageCache = System.Collections.Concurrent.ConcurrentDictionary<Disqord.Snowflake, System.Collections.Concurrent.ConcurrentDictionary<Disqord.Snowflake, Administrator.Core.IMessageCacheService.Message>>;

namespace Administrator.Core;

public interface IMessageCacheService : IAdminService<IMessageCacheService>
{
    MessageCache Cache { get; }

    public sealed record Message(Snowflake Id, Snowflake AuthorId, string Content, IEnumerable<Uri>? Attachments)
    {
        public static Message FromMessage(IMessage message)
        {
            return new(message.Id, message.Author.Id, message.Content, (message as IUserMessage)?.Attachments.Select(x => new Uri(x.Url)));
        }
    }
}