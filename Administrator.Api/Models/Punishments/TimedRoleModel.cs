﻿using System.Text.Json.Serialization;
using Administrator.Core;
using Administrator.Database;

namespace Administrator.Api;

public sealed record TimedRoleModel(
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
        [property: JsonPropertyName("role")]
            ulong RoleId, 
        [property: JsonPropertyName("expires")]
            DateTimeOffset? ExpiresAt, 
            TimedRoleApplyMode Mode)
    : RevocablePunishmentModel(Id, Target, Moderator, CreatedAt, Reason, RevokedAt, Revoker, RevocationReason, AppealedAt, AppealText, AppealStatus)
{
    public TimedRoleModel(TimedRole timedRole)
        : this(timedRole.Id,
            timedRole.Target, 
            timedRole.Moderator,
            timedRole.CreatedAt, 
            timedRole.Reason,
            timedRole.RevokedAt, 
            timedRole.Revoker,
            timedRole.RevocationReason,
            timedRole.AppealedAt,
            timedRole.AppealText,
            timedRole.AppealStatus,
            timedRole.RoleId,
            timedRole.ExpiresAt,
            timedRole.Mode)
    { }
}