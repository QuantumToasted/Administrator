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

[SlashGroup("self")]
public sealed class SelfModule(AdminDbContext db, AutoCompleteService autoComplete) : DiscordApplicationModuleBase
{
    [SlashCommand("timezone")]
    [Description("Sets your timezone. Used for specifying and converting your local time into UTC.")]
    public async Task<IResult> SetTimeZoneAsync(
        [Description("The new timezone. (Pick the location closest to you.)")] 
            TimeZoneInfo timezone)
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

    [SlashCommand("demerit-points")]
    [Description("Views your current demerit points in a server.")]
    public async Task<IResult> ViewDemeritPointsAsync(
        [Name("server-id")]
        [Description("The ID of the server to view demerit points for. Defaults to the current server.")]
            Snowflake? guildId = null)
    {
        guildId ??= Context.GuildId;
        if (!guildId.HasValue)
            return Response("If not specifying a server ID, this command must be used in a server.").AsEphemeral(Context.GuildId.HasValue);

        var member = await db.Members.FirstOrDefaultAsync(x => x.GuildId == guildId.Value && x.UserId == Context.AuthorId);
        if (member is null)
            return Response("You do not have any demerit points in that server, or a server with that ID doesn't exist.").AsEphemeral(Context.GuildId.HasValue);
        
        var responseBuilder = new StringBuilder()
            .AppendNewline($"You are currently at {Markdown.Bold("demerit point".ToQuantity(member.DemeritPoints))} " +
                           $"in {Markdown.Bold(Bot.GetGuild(guildId.Value)!.Name)}.");
        var guild = await db.Guilds.GetOrCreateAsync(guildId.Value);
        
        if (member.LastDemeritPointDecay.HasValue && guild.DemeritPointsDecayInterval.HasValue)
        {
            var nextDecay = member.LastDemeritPointDecay.Value + guild.DemeritPointsDecayInterval.Value;
            responseBuilder.AppendNewline($"Your next decay will occur {Markdown.Timestamp(nextDecay, Markdown.TimestampFormat.RelativeTime)}.");
        }
        
        return Response(responseBuilder.ToString()).AsEphemeral(Context.GuildId.HasValue);
    }

    [SlashCommand("punishments")]
    [Description("Views your current punishments in a server.")]
    public async Task<IResult> ViewPunishmentsAsync(
        [Name("server-id")]
        [Description("The ID of the server to view punishments for. Defaults to the current server.")]
            Snowflake? guildId = null)
    {
        guildId ??= Context.GuildId;
        if (!guildId.HasValue)
            return Response("If not specifying a server ID, this command must be used in a server.").AsEphemeral(Context.GuildId.HasValue);
        
        var guild = Bot.GetGuild(guildId.Value)!;
        
        var pages = await PunishmentsModule.GeneratePagesAsync(db, Context, $"Your punishments in {guild.Name}", 
            x => x.Target.Id == Context.AuthorId.RawValue, guildId);
            
        return pages.Count switch
        {
            0 => Response("No punishments could be found in that server, or a server with that ID doesn't exist.").AsEphemeral(Context.GuildId.HasValue),
            1 => Response(pages[0].Embeds.Value[0]).AsEphemeral(Context.GuildId.HasValue),
            _ => Menu(new AdminInteractionMenu(new AdminPagedView(pages, Context.GuildId.HasValue), Context.Interaction))
        };
    }

    [AutoComplete("timezone")]
    public void AutoCompleteTimezones(AutoComplete<string> timezone)
    {
        if (!timezone.IsFocused)
            return;
        
        autoComplete.AutoComplete(timezone, DateTimeExtensions.IanaTimeZoneMap.Values.ToList());
    }
}