using System.Text;
using Administrator.Core;
using Administrator.Database;
using Disqord;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Timeout = Administrator.Database.Timeout;
using static Microsoft.AspNetCore.Http.Results;

namespace Administrator.Api;

public static class EndpointExtensions
{
    public static IEndpointRouteBuilder MapPunishments(this IEndpointRouteBuilder builder)
    {
        const string route = "/api/punishments/{guildId}";
        
        builder.MapGet(route, async (
            [FromRoute] long guildId,
            [FromServices] AdminDbContext db,
            [FromHeader(Name = "Authorization")] string? apiKey = null,
            [FromQuery] int? start = null, 
            [FromQuery(Name = "moderator")] long? moderatorId = null, 
            [FromQuery(Name = "target")] long? targetId = null,
            [FromQuery(Name = "type")] string? type = null) =>
        {
            Snowflake actualGuildId = (ulong)guildId;

            if (!await AuthorizeGuildAsync(actualGuildId, apiKey, db))
                return Unauthorized();
            
            start = Math.Max(0, start ?? 0);

            var query = db.Punishments.Where(x => x.GuildId == actualGuildId && x.Id > start.Value);

            if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<PunishmentType>(type, true, out var punishmentType))
            {
                query = punishmentType switch
                {
                    PunishmentType.Ban => query.OfType<Ban>(),
                    PunishmentType.Block => query.OfType<Block>(),
                    PunishmentType.Kick => query.OfType<Kick>(),
                    PunishmentType.TimedRole => query.OfType<TimedRole>(),
                    PunishmentType.Timeout => query.OfType<Timeout>(),
                    PunishmentType.Warning => query.OfType<Warning>(),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }

            if (moderatorId.HasValue)
            {
                var actualModeratorId = (ulong)moderatorId.Value;
                query = query.Where(x => x.Moderator.Id == actualModeratorId);
            }
            
            if (targetId.HasValue)
            {
                var actualTargetId = (ulong)targetId.Value;
                query = query.Where(x => x.Target.Id == actualTargetId);
            }
            
            var punishments = await query.OrderBy(x => x.Id).Take(101).ToListAsync();
            var dtos = punishments.Take(100).Select(PunishmentDTO.FromPunishment).ToList();

            int? next = null;
            if (punishments.Count > 100) // there were 101 results
            {
                next = punishments[^1].Id;
            }

            return Ok(new PunishmentGetResponseDTO(dtos, next));
        });

        builder.MapGet($"{route}/{{punishmentId:int}}", async (
            [FromRoute] long guildId,
            [FromRoute] int punishmentId,
            [FromServices] AdminDbContext db,
            [FromHeader(Name = "Authorization")] string? apiKey = null) =>
        {
            Snowflake actualGuildId = (ulong)guildId;

            if (!await AuthorizeGuildAsync(actualGuildId, apiKey, db))
                return Unauthorized();

            if (await db.Punishments.FirstOrDefaultAsync(x => x.GuildId == actualGuildId && x.Id == punishmentId) is not { } punishment)
                return NotFound();

            return Ok(PunishmentDTO.FromPunishment(punishment));
        });
        
        builder.MapPost($"{route}/ban", async (
            [FromRoute] long guildId,
            [FromBody] CreateBanDTO dto,
            [FromServices] AdminDbContext db,
            [FromServices] IPunishmentService punishments,
            [FromServices] IDiscordEntityRequester requester,
            [FromHeader(Name = "Authorization")] string? apiKey = null) =>
        {
            Snowflake actualGuildId = (ulong)guildId;

            if (!await AuthorizeGuildAsync(actualGuildId, apiKey, db))
                return Unauthorized();

            if (!dto.Validate(actualGuildId, requester, out var error))
                return BadRequest(new ErrorDTO(error));

            var now = DateTimeOffset.UtcNow;
            var result = await punishments.BanAsync(actualGuildId, requester.GetUser(dto.TargetId)!, requester.GetUser(dto.ModeratorId)!, 
                dto.Reason, dto.MessagePruneDays, now + dto.Duration, null);

            if (!result.IsSuccessful)
                return BadRequest(result.ErrorMessage);

            return Ok(new BanDTO(result.Value));
        });
        
        builder.MapPost($"{route}/block", async (
            [FromRoute] long guildId,
            [FromBody] CreateBlockDTO dto,
            [FromServices] AdminDbContext db,
            [FromServices] IPunishmentService punishments,
            [FromServices] IDiscordEntityRequester requester,
            [FromHeader(Name = "Authorization")] string? apiKey = null) =>
        {
            Snowflake actualGuildId = (ulong)guildId;

            if (!await AuthorizeGuildAsync(actualGuildId, apiKey, db))
                return Unauthorized();

            if (!dto.Validate(actualGuildId, requester, out var error))
                return BadRequest(new ErrorDTO(error));

            var now = DateTimeOffset.UtcNow;
            var result = await punishments.BlockAsync(actualGuildId, requester.GetUser(dto.TargetId)!, requester.GetUser(dto.ModeratorId)!,
                dto.Reason, requester.GetChannel(actualGuildId, dto.ChannelId)!, now + dto.Duration, null);

            if (!result.IsSuccessful)
                return BadRequest(result.ErrorMessage);

            return Ok(new BlockDTO(result.Value));
        });
        
        builder.MapPost($"{route}/kick", async (
            [FromRoute] long guildId,
            [FromBody] CreateKickDTO dto,
            [FromServices] AdminDbContext db,
            [FromServices] IPunishmentService punishments,
            [FromServices] IDiscordEntityRequester requester,
            [FromHeader(Name = "Authorization")] string? apiKey = null) =>
        {
            Snowflake actualGuildId = (ulong)guildId;

            if (!await AuthorizeGuildAsync(actualGuildId, apiKey, db))
                return Unauthorized();

            if (!dto.Validate(actualGuildId, requester, out var error))
                return BadRequest(new ErrorDTO(error));

            var result = await punishments.KickAsync(actualGuildId, requester.GetUser(dto.TargetId)!, requester.GetUser(dto.ModeratorId)!, dto.Reason, null);

            if (!result.IsSuccessful)
                return BadRequest(result.ErrorMessage);

            return Ok(new KickDTO(result.Value));
        });
        
        builder.MapPost($"{route}/timeout", async (
            [FromRoute] long guildId,
            [FromBody] CreateTimeoutDTO dto,
            [FromServices] AdminDbContext db,
            [FromServices] IPunishmentService punishments,
            [FromServices] IDiscordEntityRequester requester,
            [FromHeader(Name = "Authorization")] string? apiKey = null) =>
        {
            Snowflake actualGuildId = (ulong)guildId;

            if (!await AuthorizeGuildAsync(actualGuildId, apiKey, db))
                return Unauthorized();

            if (!dto.Validate(actualGuildId, requester, out var error))
                return BadRequest(new ErrorDTO(error));

            var now = DateTimeOffset.UtcNow;
            var result = await punishments.TimeoutAsync(actualGuildId, requester.GetUser(dto.TargetId)!, requester.GetUser(dto.ModeratorId)!,
                dto.Reason, now + dto.Duration, null);

            if (!result.IsSuccessful)
                return BadRequest(result.ErrorMessage);

            return Ok(new TimeoutDTO(result.Value));
        });
        
        builder.MapPost($"{route}/timedRole", async (
            [FromRoute] long guildId,
            [FromBody] CreateTimedRoleDTO dto,
            [FromServices] AdminDbContext db,
            [FromServices] IPunishmentService punishments,
            [FromServices] IDiscordEntityRequester requester,
            [FromHeader(Name = "Authorization")] string? apiKey = null) =>
        {
            Snowflake actualGuildId = (ulong)guildId;

            if (!await AuthorizeGuildAsync(actualGuildId, apiKey, db))
                return Unauthorized();

            if (!dto.Validate(actualGuildId, requester, out var error))
                return BadRequest(new ErrorDTO(error));

            var now = DateTimeOffset.UtcNow;
            var result = dto.Mode == TimedRoleApplyMode.Grant
                ? await punishments.GrantTimedRoleAsync(actualGuildId, requester.GetUser(dto.TargetId)!, requester.GetUser(dto.ModeratorId)!, dto.Reason, requester.GetRole(actualGuildId, dto.TargetId)!, now + dto.Duration, null)
                : await punishments.RevokeTimedRoleAsync(actualGuildId, requester.GetUser(dto.TargetId)!, requester.GetUser(dto.ModeratorId)!, dto.Reason, requester.GetRole(actualGuildId, dto.TargetId)!, now + dto.Duration, null);

            if (!result.IsSuccessful)
                return BadRequest(result.ErrorMessage);

            return Ok(new TimedRoleDTO(result.Value));
        });
        
        builder.MapPost($"{route}/warning", async (
            [FromRoute] long guildId,
            [FromBody] CreateWarningDTO dto,
            [FromServices] AdminDbContext db,
            [FromServices] IPunishmentService punishments,
            [FromServices] IDiscordEntityRequester requester,
            [FromHeader(Name = "Authorization")] string? apiKey = null) =>
        {
            Snowflake actualGuildId = (ulong)guildId;

            if (!await AuthorizeGuildAsync(actualGuildId, apiKey, db))
                return Unauthorized();

            if (!dto.Validate(actualGuildId, requester, out var error))
                return BadRequest(new ErrorDTO(error));

            var result = await punishments.WarnAsync(actualGuildId, requester.GetUser(dto.TargetId)!, requester.GetUser(dto.ModeratorId)!,
                dto.Reason, dto.DemeritPoints, null);

            if (!result.IsSuccessful)
                return BadRequest(result.ErrorMessage);

            return Ok(new WarningDTO(result.Value));
        });
        
        builder.MapPost($"{route}/revoke/{{punishmentId:int}}", async (
            [FromRoute] long guildId,
            [FromRoute] int punishmentId,
            [FromBody] RevokePunishmentDTO dto,
            [FromServices] AdminDbContext db,
            [FromServices] IPunishmentService punishments,
            [FromServices] IDiscordEntityRequester requester,
            [FromHeader(Name = "Authorization")] string? apiKey = null) =>
        {
            Snowflake actualGuildId = (ulong)guildId;

            if (!await AuthorizeGuildAsync(actualGuildId, apiKey, db))
                return Unauthorized();

            if (!dto.Validate(actualGuildId, requester, out var error))
                return BadRequest(new ErrorDTO(error));

            if (await db.Punishments.FirstOrDefaultAsync(x => x.GuildId == actualGuildId && x.Id == punishmentId) is not IRevocablePunishment punishment)
                return NotFound();

            if (punishment.RevokedAt.HasValue)
                return BadRequest(new ErrorDTO($"Punishment {punishment.Id} is already revoked."));

            var result = await punishments.RevokePunishmentAsync(actualGuildId, punishmentId, requester.GetUser(dto.RevokerId)!, dto.Reason);

            if (!result.IsSuccessful)
                return BadRequest(result.ErrorMessage);

            return Ok(PunishmentDTO.FromPunishment(result.Value));
        });
        
        return builder;
    }

    private static async Task<bool> AuthorizeGuildAsync(Snowflake guildId, string? apiKey, AdminDbContext db)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return false;
        
        var split = apiKey.Split('.');
        if (split.Length != 2)
            return false;

        Snowflake keyGuildId;
        try
        {
            keyGuildId = Snowflake.Parse(Encoding.Default.GetString(Convert.FromBase64String(split[0])));
        }
        catch
        {
            return false;
        }
        
        if (guildId != keyGuildId)
            return false;

        byte[] cryptoBytes;
        try
        {
            cryptoBytes = Convert.FromBase64String(split[1]);
        }
        catch
        {
            return false;
        }
        
        var guild = await db.Guilds.GetOrCreateAsync(guildId);
        if (guild.ApiKeySalt is null || guild.ApiKeyHash is null)
            return false;

        return guild.CheckApiKeyHash(cryptoBytes);
    }
}