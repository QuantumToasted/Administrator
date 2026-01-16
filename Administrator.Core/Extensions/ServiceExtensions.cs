using Disqord;
using Message = Administrator.Core.IMessageCacheService.Message;

namespace Administrator.Core;

public static class ServiceExtensions
{
    extension<TService>(TService service) where TService : IMessageCacheService
    {
        public void AddOrUpdateMessage(Snowflake channelId, Message message)
        {
            var cache = service.Cache.GetOrAdd(channelId, _ => []);
            cache.AddOrUpdate(message.Id, _ => message, (_, m) => m with { Content = message.Content });
        }
    }
}