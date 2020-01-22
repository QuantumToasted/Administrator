using System;
using System.Net.Http;
using System.Threading.Tasks;
using Backpack.Net;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using SteamWebAPI2.Exceptions;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;

namespace Administrator.Commands
{
    public sealed class BackpackUserParser : TypeParser<BackpackUser>
    {
        public override async ValueTask<TypeParserResult<BackpackUser>> ParseAsync(Parameter parameter, string value, CommandContext ctx)
        {
            var context = (AdminCommandContext) ctx;
            var factory = ctx.ServiceProvider.GetRequiredService<SteamWebInterfaceFactory>();
            var client = ctx.ServiceProvider.GetRequiredService<BackpackClient>();

            if (!ulong.TryParse(value, out var steamId))
            {
                var split = value.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (split.Length > 0)
                {
                    var last = split[^1];

                    if (!ulong.TryParse(last, out steamId))
                    {
                        try
                        {
                            var steamUser = factory.CreateSteamWebInterface<SteamUser>(ctx.ServiceProvider.GetRequiredService<HttpClient>());
                            var response = await steamUser.ResolveVanityUrlAsync(last);
                            steamId = response.Data;
                        }
                        catch (VanityUrlNotResolvedException)
                        { }
                    }
                }
            }

            var users = await client.GetUsersAsync(steamId);
            return !users.IsSuccess || users.Users.IsDefaultOrEmpty
                ? TypeParserResult<BackpackUser>.Unsuccessful(context.Localize("backpackuserparser_notfound"))
                : TypeParserResult<BackpackUser>.Successful(users.Users[0]);
        }
    }
}