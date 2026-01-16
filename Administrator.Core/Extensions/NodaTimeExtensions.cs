using NodaTime;

namespace Administrator.Core;

public static class NodaTimeExtensions
{
    extension(Instant)
    {
        public static Instant Now => SystemClock.Instance.GetCurrentInstant();
    }
}