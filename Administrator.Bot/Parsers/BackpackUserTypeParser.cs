using System.Text;
using Backpack.Net;
using Disqord;
using Disqord.Bot.Commands;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using SteamWebAPI2.Exceptions;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;

namespace Administrator.Bot;

public sealed class BackpackUserTypeParser : DiscordTypeParser<BackpackUser>
{
    public override async ValueTask<ITypeParserResult<BackpackUser>> ParseAsync(IDiscordCommandContext context, IParameter parameter, ReadOnlyMemory<char> value)
    {
        var factory = context.Services.GetRequiredService<ISteamWebInterfaceFactory>();
        var backpack = context.Services.GetRequiredService<BackpackClient>();

        if (!ulong.TryParse(value.Span, out var steamId))
        {
            var split = new string(value.Span).Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (split.Length > 0)
            {
                var last = split[^1];
                if (!ulong.TryParse(last, out steamId))
                {
                    try
                    {
                        var steamUser = factory.CreateSteamWebInterface<SteamUser>(context.Services.GetRequiredService<HttpClient>());
                        var response = await steamUser.ResolveVanityUrlAsync(last);
                        steamId = response.Data;
                    }
                    catch (VanityUrlNotResolvedException)
                    {
                        return Failure($"The supplied Steam ID or profile link {Markdown.Code(value)} was invalid.");
                    }
                }
            }
        }

        try
        {
            var users = await backpack.GetUsersAsync(steamId);

            if (!users.IsSuccess)
            {
                var failureBuilder = new StringBuilder()
                    .AppendNewline("An error occurred with the backpack.tf API.");

                if (!string.IsNullOrWhiteSpace(users.ErrorMessage))
                    failureBuilder.AppendNewline(users.ErrorMessage);

                if (!string.IsNullOrWhiteSpace(users.Reason))
                    failureBuilder.AppendNewline(users.Reason);

                return Failure(failureBuilder.ToString());
            }

            return users.Users.Count > 0
                ? Success(users.Users[0])
                : Failure($"The supplied Steam ID or profile link {Markdown.Code(value)} was invalid.");
        }
        catch (Exception ex)
        {
            return Failure("The backpack.tf API returned an invalid response when trying to get this user's profile.\n" +
                           "Please try again later.");
        }
    }
}