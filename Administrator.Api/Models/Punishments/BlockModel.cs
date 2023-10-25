using System.Text.Json.Serialization;
using Administrator.Database;

namespace Administrator.Api;

public sealed record BlockModel(
            int Id, 
            UserModel Target, 
            UserModel Moderator, 
            DateTimeOffset CreatedAt, 
            string? Reason, 
            DateTimeOffset? RevokedAt, 
            UserModel? Revoker, 
            string? RevocationReason, 
            DateTimeOffset? AppealedAt, 
            string? AppealText, 
            AppealStatus? AppealStatus,
        [property: JsonPropertyName("channel")]
            ulong ChannelId, 
        [property: JsonPropertyName("expires")]
            DateTimeOffset? ExpiresAt)
    : RevocablePunishmentModel(Id, Target, Moderator, CreatedAt, Reason, RevokedAt, Revoker, RevocationReason, AppealedAt, AppealText, AppealStatus)
{
    public BlockModel(Block block)
        : this(block.Id,
            new UserModel(block.TargetId, block.TargetName),
            new UserModel(block.ModeratorId, block.ModeratorName),
            block.CreatedAt, 
            block.Reason,
            block.RevokedAt, 
            block.RevokerId.HasValue 
                ? new UserModel(block.RevokerId.Value, block.RevokerName!)
                : null,
            block.RevocationReason,
            block.AppealedAt,
            block.AppealText,
            block.AppealStatus,
            block.ChannelId,
            block.ExpiresAt)
    { }
}