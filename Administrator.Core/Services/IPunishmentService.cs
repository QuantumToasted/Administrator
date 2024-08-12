using Disqord;

namespace Administrator.Core;

public interface IPunishmentService
{
    Task<Result<IBan>> BanAsync(Snowflake guildId, IUser target, IUser moderator, string? reason, int? messagePruneDays, DateTimeOffset? expiresAt, IAttachment? attachment);
    Task<Result<IBlock>> BlockAsync(Snowflake guildId, IUser target, IUser moderator, string? reason, IChannel channel, DateTimeOffset? expiresAt, IAttachment? attachment);
    Task<Result<IKick>> KickAsync(Snowflake guildId, IUser target, IUser moderator, string? reason, IAttachment? attachment);
    Task<Result<ITimedRole>> GrantTimedRoleAsync(Snowflake guildId, IUser target, IUser moderator, string? reason, IRole role, DateTimeOffset? expiresAt, IAttachment? attachment);
    Task<Result<ITimedRole>> RevokeTimedRoleAsync(Snowflake guildId, IUser target, IUser moderator, string? reason, IRole role, DateTimeOffset? expiresAt, IAttachment? attachment);
    Task<Result<ITimeout>> TimeoutAsync(Snowflake guildId, IUser target, IUser moderator, string? reason, DateTimeOffset expiresAt, IAttachment? attachment);
    Task<Result<IWarning>> WarnAsync(Snowflake guildId, IUser target, IUser moderator, string? reason, int? demeritPoints, IAttachment? attachment);
    Task<Result<IRevocablePunishment>> RevokePunishmentAsync(Snowflake guildId, int punishmentId, IUser revoker, string? reason);
}