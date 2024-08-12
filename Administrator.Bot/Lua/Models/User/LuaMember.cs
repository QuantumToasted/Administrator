using Disqord;
using Disqord.Gateway;
using Disqord.Rest;
using Laylua.Marshaling;
using Qommon;

namespace Administrator.Bot;

public sealed class LuaMember(IMember member, DiscordLuaLibraryBase library) : LuaUser(member), ILuaModel<LuaMember>
{
    //public long GuildId { get; } = (long) member.GuildId.RawValue;
    
    public long[] Roles { get; } = member.RoleIds.Except([member.GuildId]).Select(x => (long) x.RawValue).ToArray();
    
    public string? Joined { get; } = (member.JoinedAt.GetValueOrNullable() ?? DateTimeOffset.UtcNow).ToString("s");
    
    public string? Nickname { get; } = member.Nick;
    
    public bool Muted { get; } = member.IsMuted;
    
    public bool Deafened { get; } = member.IsDeafened;
    
    public string? Boosted { get; } = member.BoostedAt?.ToString("s");
    
    public bool Pending { get; } = member.IsPending;

    public string GuildAvatar { get; } = member.GetGuildAvatarUrl(CdnAssetFormat.Automatic, 1024);
    
    public string? TimedOutUntil { get; } = member.TimedOutUntil?.ToString("s");
    
    //public long MemberFlags { get; } = (long) member.GuildFlags;

    public long Permissions { get; } = (long) member.CalculateGuildPermissions();

    public bool HasPermission(long permissions)
        => ((Permissions) Permissions).HasFlag((Permissions) permissions);

    public void SetNickname(string nick)
    {
        Guard.IsNotNull(nick);
        library.RunWait(ct => member.ModifyAsync(x => x.Nick = nick, cancellationToken: ct));
    }

    public bool Kick(string? reason)
    {
        reason = !string.IsNullOrWhiteSpace(reason) ? reason : "No reason.";
        try
        {
            library.RunWait(ct => member.KickAsync(new DefaultRestRequestOptions().WithReason(reason), cancellationToken: ct));
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    public bool Ban(string? reason, int? pruneDays)
    {
        reason = !string.IsNullOrWhiteSpace(reason) ? reason : "No reason.";
        pruneDays = Math.Max(0, pruneDays.GetValueOrDefault());
        try
        {
            library.RunWait(ct => member.BanAsync(reason, pruneDays, cancellationToken: ct));
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void GrantRole(long roleId)
        => library.RunWait(ct => member.GrantRoleAsync((ulong)roleId, cancellationToken: ct));
    
    public void RevokeRole(long roleId)
        => library.RunWait(ct => member.RevokeRoleAsync((ulong)roleId, cancellationToken: ct));
    
    static void ILuaModel.SetUserDataDescriptor(DefaultUserDataDescriptorProvider provider)
        => ILuaModel<LuaMember>.SetUserDataDescriptor(provider);
}