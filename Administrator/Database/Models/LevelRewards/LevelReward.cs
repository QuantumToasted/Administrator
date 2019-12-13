using System.Threading.Tasks;
using Disqord;

namespace Administrator.Database
{
    public abstract class LevelReward
    {
        private LevelReward()
        { }

        protected LevelReward(ulong guildId, int level, int tier)
        {
            GuildId = guildId;
            Level = level;
            Tier = tier;
        }

        public int Id { get; set; }

        public ulong GuildId { get; set; }

        public int Level { get; set; }

        public int Tier { get; set; }

        public abstract Task RewardAsync(CachedMember member);
    }
}