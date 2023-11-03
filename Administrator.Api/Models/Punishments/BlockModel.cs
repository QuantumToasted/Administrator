using System.Text.Json.Serialization;
using Administrator.Core;
using Administrator.Database;

namespace Administrator.Api;

public sealed record BlockModel(
            int Id, 
            UserSnapshot Target, 
            UserSnapshot Moderator, 
            DateTimeOffset CreatedAt, 
            string? Reason, 
            DateTimeOffset? RevokedAt, 
            UserSnapshot? Revoker, 
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
            block.Target,
            block.Moderator,
            block.CreatedAt, 
            block.Reason,
            block.RevokedAt, 
            block.Revoker,
            block.RevocationReason,
            block.AppealedAt,
            block.AppealText,
            block.AppealStatus,
            block.ChannelId,
            block.ExpiresAt)
    { }
}