using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disqord;

namespace Administrator.Database
{
    public sealed class RoleLevelReward : LevelReward
    {
        private RoleLevelReward(ulong guildId, int level, int tier)
            : base(guildId, level, tier)
        { }

        public RoleLevelReward(ulong guildId, int level, int tier, IEnumerable<IRole> addedRoles, IEnumerable<IRole> removedRoles)
            : base(guildId, level, tier)
        {
            AddedRoleIds = addedRoles.Select(x => x.Id.RawValue).ToList();
            RemovedRoleIds = removedRoles.Select(x => x.Id.RawValue).ToList();
        }

        public List<ulong> AddedRoleIds { get; set; }

        public List<ulong> RemovedRoleIds { get; set; }

        public override async Task RewardAsync(CachedMember member)
        {
            foreach (var id in AddedRoleIds)
            {
                await member.GrantRoleAsync(id);
            }

            foreach (var id in RemovedRoleIds)
            {
                await member.RevokeRoleAsync(id);
            }
        }
    }
}