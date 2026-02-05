using Disqord;

namespace Administrator.Core;

public enum ReminderRepeatMode
{
    Daily = 1,
    Weekly,
    Monthly
}

public interface IReminder : IKeyedEntity<int>, IChannelEntity, IExpiringEntity
{
    
}