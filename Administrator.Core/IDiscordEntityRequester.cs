using Disqord;

namespace Administrator.Core;

public interface IDiscordEntityRequester
{
    Task<IDirectChannel> FetchDirectChannelAsync(Snowflake userId);
    IUser? GetUser(Snowflake userId);
    IMember? GetMember(Snowflake guildId, Snowflake memberId);
    IRole? GetRole(Snowflake guildId, Snowflake roleId);
    IGuildChannel? GetChannel(Snowflake guildId, Snowflake channelId);
}