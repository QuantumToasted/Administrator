using System.Globalization;
using System.Text;
using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Gateway;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Qmmands;

namespace Administrator.Bot;

public sealed partial class SelfModule(AdminDbContext db, AutoCompleteService autoComplete) : DiscordApplicationModuleBase
{
    public partial async Task<IResult> Timezone(TimeZoneInfo timezone)
    {
        var globalUser = await db.Users.GetOrCreateAsync(Context.AuthorId);
        globalUser.TimeZone = timezone;
        await db.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        var discordTimestamp = Markdown.Timestamp(now, Markdown.TimestampFormat.ShortTime);
        var actualTimestamp = TimeZoneInfo.ConvertTime(now, timezone)
            .ToString("h:mm tt", CultureInfo.InvariantCulture);

        var responseBuilder = new StringBuilder()
            .AppendNewline("Timezone updated! If you set the right timezone, these two should look identical:")
            .AppendNewline(discordTimestamp)
            .AppendNewline(actualTimestamp);

        return Response(responseBuilder.ToString()).AsEphemeral(Context.GuildId.HasValue);
    }

    public partial async Task<IResult> DemeritPoints(Snowflake? guildId)
    {
        guildId ??= Context.GuildId;
        if (!guildId.HasValue)
            return Response("If not specifying a server ID, this command must be used in a server.").AsEphemeral(Context.GuildId.HasValue);

        var member = await db.Members.FirstOrDefaultAsync(x => x.GuildId == guildId.Value && x.UserId == Context.AuthorId);
        var currentDemeritPoints = await db.Punishments.GetCurrentDemeritPointsAsync(guildId.Value, Context.AuthorId);
        
        if (member is null || currentDemeritPoints == 0 || Bot.GetGuild(guildId.Value) is not { } guild)
            return Response("You do not have any demerit points in that server, or a server with that ID doesn't exist.").AsEphemeral(Context.GuildId.HasValue);
        
        var responseBuilder = new StringBuilder()
            .AppendNewline($"You are currently at {Markdown.Bold("demerit point".ToQuantity(currentDemeritPoints))} " +
                           $"in {Markdown.Bold(guild.Name)}.");
        
        var dbGuild = await db.Guilds.GetOrCreateAsync(guildId.Value);
        
        if (member.NextDemeritPointDecay.HasValue && dbGuild.DemeritPointsDecayInterval.HasValue)
        {
            var nextDecay = member.NextDemeritPointDecay.Value + dbGuild.DemeritPointsDecayInterval.Value;
            responseBuilder.AppendNewline($"Your next decay will occur {Markdown.Timestamp(nextDecay, Markdown.TimestampFormat.RelativeTime)}.");
        }
        
        return Response(responseBuilder.ToString()).AsEphemeral(Context.GuildId.HasValue);
    }

    public partial async Task<IResult> Punishments(Snowflake? guildId)
    {
        guildId ??= Context.GuildId;
        if (!guildId.HasValue)
            return Response("If not specifying a server ID, this command must be used in a server.").AsEphemeral(Context.GuildId.HasValue);
        
        var guild = Bot.GetGuild(guildId.Value)!;
        
        var pages = await PunishmentsModule.GeneratePagesAsync(db, Context, $"Your punishments in {guild.Name}", 
            x => x.Target.Id == (ulong) Context.AuthorId, guildId);
            
        return pages.Count switch
        {
            0 => Response("No punishments could be found in that server, or a server with that ID doesn't exist.").AsEphemeral(Context.GuildId.HasValue),
            1 => Response(pages[0].Embeds.Value[0]).AsEphemeral(Context.GuildId.HasValue),
            _ => Menu(new AdminInteractionMenu(new AdminPagedView(pages, Context.GuildId.HasValue), Context.Interaction))
        };
    }

    public partial void AutoCompleteTimezones(AutoComplete<string> timezone)
    {
        if (!timezone.IsFocused)
            return;
        
        autoComplete.AutoComplete(timezone, DateTimeExtensions.IanaTimeZoneMap.Values.ToList());
    }
}