using Disqord;
using Disqord.Rest;
using Laylua;
using Qommon;

namespace Administrator.Bot;

public sealed partial class DiscordRestLuaLibrary
{
    private partial IEnumerable<string> RegisterUserRestMethods(Lua lua)
    {
        yield return lua.SetStringGlobal("banUser", (Action<long, string?, int?>)BanUser);
        yield return lua.SetStringGlobal("kickUser", (Action<long, string?>)KickUser);
        yield return lua.SetStringGlobal("modifyUserNick", (Action<long, string?>)ModifyUserNick);
        yield return lua.SetStringGlobal("grantUserRole", (Action<long, long>)GrantUserRole);
        yield return lua.SetStringGlobal("revokeUserRole", (Action<long, long>)RevokeUserRole);
    }
    
    private void BanUser(long userId, string? reason = null, int? pruneDays = null)
    {
        if (pruneDays.HasValue)
            Guard.IsBetweenOrEqualTo(pruneDays.Value, 0, 7);
        
        if (reason is not null)
            Guard.HasSizeLessThanOrEqualTo(reason, Discord.Limits.Rest.MaxAuditLogReasonLength);
        
        RunWait(() => bot.CreateBanAsync(guildId, (ulong) userId, reason, pruneDays));
    }

    private void KickUser(long userId, string? reason = null)
    {
        if (reason is not null)
            Guard.HasSizeLessThanOrEqualTo(reason, Discord.Limits.Rest.MaxAuditLogReasonLength);

        RunWait(() => bot.KickMemberAsync(guildId, (ulong)userId,
            reason is not null ? new DefaultRestRequestOptions().WithReason(reason) : null));
    }

    private void ModifyUserNick(long userId, string? nick)
    {
        nick ??= string.Empty;
        Guard.HasSizeLessThanOrEqualTo(nick, 32);
        
        RunWait(() => bot.ModifyMemberAsync(guildId, (ulong)userId, x => x.Nick = nick));
    }

    private void GrantUserRole(long userId, long roleId)
    {
        RunWait(() => bot.GrantRoleAsync(guildId, (ulong)userId, (ulong)roleId));
    }

    private void RevokeUserRole(long userId, long roleId)
    {
        RunWait(() => bot.RevokeRoleAsync(guildId, (ulong)userId, (ulong)roleId));
    }
}