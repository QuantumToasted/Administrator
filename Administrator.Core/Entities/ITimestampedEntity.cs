using NodaTime;

namespace Administrator.Core;

public interface ITimestampedEntity
{
    Instant Timestamp { get; }
}