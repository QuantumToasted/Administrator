using Disqord;
using Disqord.Gateway;

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
}