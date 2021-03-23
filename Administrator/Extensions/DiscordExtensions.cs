using System;
using System.Threading.Tasks;
using Disqord;
using Disqord.Gateway;
using Disqord.Rest;

namespace Administrator.Extensions
{
    public static class DiscordExtensions
    {
        public static async ValueTask<IUser> GetOrFetchUserAsync(this DiscordClientBase client, Snowflake id)
        {
            if (client.GetUser(id) is { } cachedUser)
                return cachedUser;

            return await client.FetchUserAsync(id);
        }
        
        public static int GetEmojiLimit(this IGuild guild)
        {
            return guild.BoostTier switch
            {
                BoostTier.None => 50,
                BoostTier.First => 100,
                BoostTier.Second => 150,
                BoostTier.Third => 250,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}