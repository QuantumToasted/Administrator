namespace Administrator.Database;

public interface IExpiringDbEntity
{
    DateTimeOffset? ExpiresAt { get; }
}