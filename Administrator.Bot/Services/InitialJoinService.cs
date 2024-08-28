using System.Text;
using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Disqord.Rest;
using Humanizer;
using Microsoft.Extensions.Options;

namespace Administrator.Bot;

public sealed class InitialJoinService(SlashCommandMentionService mention, IOptions<AdministratorHelpConfiguration> options) : DiscordBotService
{
    public static readonly IReadOnlyDictionary<Permissions, string> ExtraRequiredPermissions = new Dictionary<Permissions, string>
    {
        [Permissions.ManageGuild] = "Required for the bot to exempt server invites from the invite whitelist.",
        [Permissions.ViewAuditLog] = "Required for the automatic punishment detection feature to function."
    };

    private readonly AdministratorHelpConfiguration _config = options.Value;

#if !MIGRATING
    protected override async ValueTask OnJoinedGuild(JoinedGuildEventArgs e)
    {
        await TrySendInitialJoinMessageAsync(e.Guild);
    }
    

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Bot.WaitUntilReadyAsync(stoppingToken);
        
        foreach (var guild in Bot.GetGuilds().Values)
        {
            if (await TrySendInitialJoinMessageAsync(guild))
                await Task.Delay(TimeSpan.FromSeconds(Random.Shared.Next(1, 10)), stoppingToken);
        }
    }
#endif

    private async ValueTask<bool> TrySendInitialJoinMessageAsync(IGuild guild)
    {
        await Bot.WaitUntilReadyAsync(Bot.StoppingToken);
        
        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);

        var guildConfig = await db.Guilds.GetOrCreateAsync(guild.Id);
        if (guildConfig.WasVisited)
            return false;

        var message = FormatInitialJoinMessage(guild);
        var guildOwnerDmChannel = await Bot.CreateDirectChannelAsync(guild.OwnerId);
        var botMember = guild.GetMember(Bot.CurrentUser.Id)!;

        var globalUser = await db.Users.GetOrCreateAsync(guild.OwnerId);

        if (globalUser.WasSentInitialJoinMessage || await Bot.TrySendMessageAsync(guildOwnerDmChannel.Id, message) is null)
        {
            var failedCounter = 0;
            foreach (var channel in guild.GetChannels().Values.OfType<ITextChannel>()
                         .Where(x => botMember.CalculateChannelPermissions(x).HasFlag(Permissions.SendMessages))
                         .OrderBy(x => x.Position))
            {
                if (await Bot.TrySendMessageAsync(channel.Id, message) is not null)
                    break;

                if (++failedCounter >= 5)
                    break;
            }
        }

        globalUser.WasSentInitialJoinMessage = true;

        guildConfig.WasVisited = true;
        await db.SaveChangesAsync();
        return true;
    }

    private LocalMessage FormatInitialJoinMessage(IGuild guild)
    {
        var contentBuilder = new StringBuilder()
            .AppendNewline($"Thanks for inviting me, the Administrator, to your server, {Markdown.Bold(guild.Name)}!")
            .AppendNewline($"To get your server set up with me quickly, utilize the {mention.GetMention("config")} command.")
            .AppendNewline("There are a lot of things to configure and set up, and a lot of features to take advantage of.")
            .Append("If you have any questions, feel free to join my support server: ")
            .AppendNewline($"https://discord.gg/{_config.SupportGuildInviteCode}");
        
        var actualBotPermissions = guild.GetMember(Bot.CurrentUser.Id)!.CalculateGuildPermissions();

        var missingBotPermissions = Bot.Commands.GetRequiredBotPermissions() & ~actualBotPermissions;
        if (missingBotPermissions != Permissions.None)
        {
            contentBuilder.AppendNewline()
                .AppendNewline(
                    "Please note that I am missing the following required permissions for some of my commands to work correctly:")
                .AppendNewline(missingBotPermissions.Humanize(LetterCasing.Title));
        }

        var extraPermissionsNeeded = false;
        foreach (var (permissions, reason) in ExtraRequiredPermissions)
        {
            if (actualBotPermissions.HasFlag(permissions))
                continue;

            if (!extraPermissionsNeeded)
            {
                extraPermissionsNeeded = true;
                contentBuilder.AppendNewline()
                    .AppendNewline("I also require the following permissions for certain non-command functionality to work correctly:");
            }

            contentBuilder.AppendNewline($"{permissions.Humanize(LetterCasing.Title)}: {reason}");
        }

        return new LocalMessage().WithContent(contentBuilder.ToString());
    }
}