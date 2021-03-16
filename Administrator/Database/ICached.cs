using System;

namespace Administrator.Database
{
    public interface ICached
    {
        string CacheKey { get; }
        
        TimeSpan SlidingExpiration { get; }
    }
}