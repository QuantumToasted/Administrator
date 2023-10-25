using Administrator.Database;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Administrator.Api;

public static class EndpointExtensions
{
    public static IEndpointRouteBuilder MapPunishments(this IEndpointRouteBuilder builder, string route = "/api/punishments/{guildId}")
    {
        builder.MapGet(route, async (long guildId, AdminDbContext db) =>
        {
            var punishments = await db.Punishments.Where(x => x.GuildId == (ulong)guildId).ToListAsync();
            return punishments.Select(PunishmentModel.FromPunishment).OrderByDescending(x => x.Id).ToList();
        });

        return builder;
    }
}