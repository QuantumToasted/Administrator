using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using LinqToDB.EntityFrameworkCore;

namespace Administrator.Bot;

public static partial class DiscordExtensions
{
    public static CachedRole? GetHighestRole(this IMember member, Func<CachedRole, bool>? func = null)
    {
        return member.GetRoles().Values
            .Where(func ?? (_ => true))
            .OrderByDescending(x => x.Position)
            .ThenByDescending(x => x.Id)
            .FirstOrDefault();
    }
    
    public static string GetDisplayName(this IMember member)
    {
        if (!string.IsNullOrWhiteSpace(member.Nick))
            return member.Nick;

        if (!string.IsNullOrWhiteSpace(member.GlobalName))
            return member.GlobalName;

        return member.Name;
    }

    public static HashSet<Snowflake> GetLevelRewardOutputRoles(this IMember member, Member? xp, IEnumerable<RoleLevelReward> allRewards)
    {
        var roleIds = member.RoleIds.ToHashSet();

        if (xp is null)
            return roleIds;

        foreach (var reward in allRewards)
        {
            if (reward.Tier > xp.GetTier() || (reward.Tier == xp.GetTier() && reward.Level > xp.GetLevel()))
                continue;
            
            foreach (var grantedId in reward.GrantedRoleIds)
            {
                roleIds.Add(grantedId);
            }

            foreach (var revokedId in reward.RevokedRoleIds)
            {
                roleIds.Remove(revokedId);
            }
        }

        return roleIds;
    }
}