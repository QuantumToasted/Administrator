using Disqord;

namespace Administrator.Core;

public enum InviteFilterExemptionType
{
    Guild = 1, // invites from TargetId (guild)
    Channel, // invites posted in TargetId (channel and channel's threads)
    Role, // invites posted from members with TargetId (role)
    User, // invites posted from TargetId (user)
    InviteCode // invites with the code InviteCode
}

public interface IInviteFilterExemption : IKeyedEntity<int>, IGuildEntity
{
    InviteFilterExemptionType ExemptionType { get; }
}