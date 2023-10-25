using Disqord;

namespace Administrator.Core;

public interface IDirectChannelRequester
{
    Task<IDirectChannel> FetchDirectChannelAsync(Snowflake userId);
}