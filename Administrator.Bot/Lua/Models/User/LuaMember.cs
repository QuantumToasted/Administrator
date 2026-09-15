using Administrator.Core;
using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using Disqord.Models;
using Disqord.Rest;
using Laylua.Marshaling;
using Qommon;

namespace Administrator.Bot;

[LuaType]
public sealed partial class LuaMember(IMember member) : LuaUser(member), IMember
{
    public string? Nickname { get; private set; }
    
    public Snowflake[] RoleIds { get; private set; }

    [LuaName("joined")]
    public DateTimeOffset JoinedAt { get; } = member.JoinedAt.GetValueOrNullable() ?? DateTimeOffset.UtcNow;
    
    [LuaName("muted")]
    public bool IsMuted { get; } = member.IsMuted;
    
    [LuaName("deafened")]
    public bool IsDeafened { get; } = member.IsDeafened;
    
    [LuaName("boosted")]
    public DateTimeOffset? BoostedAt { get; } = member.BoostedAt;
    
    public string GuildAvatar { get; } = member.GetGuildAvatarUrl(CdnAssetFormat.Automatic, 512);
    
    public DateTimeOffset? TimedOutUntil { get; } = member.TimedOutUntil;

    public async Task<bool> SetNickname(string nickname)
    {
        try
        {
            Guard.IsNotNullOrEmpty(nickname);
            await member.ModifyAsync(x => x.Nick = nickname);
            Nickname = nickname;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> GrantRole(Snowflake roleId)
    {
        try
        {
            await member.GrantRoleAsync(roleId);
            RoleIds = RoleIds.AddUnique(roleId);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RevokeRole(Snowflake roleId)
    {
        try
        {
            await member.RevokeRoleAsync(roleId);
            RoleIds = RoleIds.Except([roleId]).ToArray();
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    Snowflake IGuildEntity.GuildId => member.GuildId;
    void IJsonUpdatable<MemberJsonModel>.Update(MemberJsonModel model) => member.Update(model);
    string? IMember.Nick => Nickname;
    IReadOnlyList<Snowflake> IMember.RoleIds => RoleIds;
    Optional<DateTimeOffset> IMember.JoinedAt => JoinedAt;
    bool IMember.IsPending => member.IsPending;
    string? IMember.GuildAvatarHash => member.GuildAvatarHash;
    MemberFlags IMember.GuildFlags => member.GuildFlags;
    IAvatarDecoration? IMember.GuildAvatarDecoration => member.GuildAvatarDecoration;
    ICollectibles? IMember.GuildCollectibles => member.GuildCollectibles;
}