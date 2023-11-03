using System.Globalization;
using System.Text;
using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands.Application;
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
        var globalUser = await db.GetOrCreateGlobalUserAsync(Context.AuthorId);
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

        return Response(responseBuilder.ToString());
    }

    [AutoComplete("timezone")]
    public void AutoCompleteTimezones(AutoComplete<string> timezone)
    {
        if (!timezone.IsFocused)
            return;
        
        autoComplete.AutoComplete(timezone, DateTimeExtensions.IanaTimeZoneMap.Values.ToList());
    }
}