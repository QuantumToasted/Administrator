namespace Administrator.Core;

public interface IExpiringEntity
{
    DateTimeOffset? ExpiresAt { get; }
}