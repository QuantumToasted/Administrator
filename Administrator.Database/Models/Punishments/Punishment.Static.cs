using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public abstract partial record Punishment
{
    public static Ban Ban(Snowflake guildId, IUser target, IUser moderator, string? reason, int? messagePruneDays, DateTimeOffset? expiresAt)
    {
        return new Ban
        {
            GuildId = guildId,
            Target = UserSnapshot.FromUser(target),
            Moderator = UserSnapshot.FromUser(moderator),
            Reason = reason,
            MessagePruneDays = messagePruneDays,
            ExpiresAt = expiresAt
        };
    }

    public static Block Block(Snowflake guildId, IUser target, IUser moderator, string? reason, Snowflake channelId, OverwritePermissions? permissions, DateTimeOffset? expiresAt)
    {
        return new Block
        {
            GuildId = guildId,
            Target = UserSnapshot.FromUser(target),
            Moderator = UserSnapshot.FromUser(moderator),
            Reason = reason,
            ChannelId = channelId,
            PreviousChannelAllowPermissions = permissions?.Allowed,
            PreviousChannelDenyPermissions = permissions?.Denied,
            ExpiresAt = expiresAt
        };
    }

    public static Kick Kick(Snowflake guildId, IUser target, IUser moderator, string? reason)
    {
        return new Kick
        {
            GuildId = guildId,
            Target = UserSnapshot.FromUser(target),
            Moderator = UserSnapshot.FromUser(moderator),
            Reason = reason
        };
    }

    public static TimedRole TimedRole(Snowflake guildId, IUser target, IUser moderator, string? reason, IRole role, TimedRoleApplyMode mode, DateTimeOffset? expiresAt)
    {
        return new TimedRole
        {
            GuildId = guildId,
            Target = UserSnapshot.FromUser(target),
            Moderator = UserSnapshot.FromUser(moderator),
            Reason = reason,
            RoleId = role.Id,
            Mode = mode,
            ExpiresAt = expiresAt
        };
    }

    public static Timeout Timeout(Snowflake guildId, IUser target, IUser moderator, string? reason, DateTimeOffset expiresAt)
    {
        return new Timeout
        {
            GuildId = guildId,
            Target = UserSnapshot.FromUser(target),
            Moderator = UserSnapshot.FromUser(moderator),
            Reason = reason,
            ExpiresAt = expiresAt
        };
    }

    public static Warning Warning(Snowflake guildId, IUser target, IUser moderator, string? reason, int demeritPoints)
    {
        return new Warning
        {
            GuildId = guildId,
            Target = UserSnapshot.FromUser(target),
            Moderator = UserSnapshot.FromUser(moderator),
            Reason = reason,
            DemeritPoints = demeritPoints,
            DemeritPointsRemaining = demeritPoints
        };
    }
}