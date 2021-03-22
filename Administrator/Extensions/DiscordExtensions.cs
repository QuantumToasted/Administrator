using System;
using Disqord;

namespace Administrator.Extensions
{
    public static class DiscordExtensions
    {
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