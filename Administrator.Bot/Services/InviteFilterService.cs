using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Disqord.Rest;
using Microsoft.Extensions.Logging;

namespace Administrator.Bot;

public sealed class InviteFilterService : DiscordBotService
{
    private static readonly Regex InviteRegex = new(
        @"(?:https?:\/\/)?(?:\w+\.)?discord(?:(?:app)?\.com\/invite|\.gg)\/([A-Za-z0-9-]+)",
        RegexOptions.Compiled);

    private readonly ConcurrentDictionary<Snowflake, HashSet<string>> _guildInviteCodes = new();

    public override int Priority => 1;

    public HashSet<Snowflake> DeletedMessageIds { get; } = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Bot.WaitUntilReadyAsync(stoppingToken);
        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);

        foreach (var guildId in Bot.GetGuilds().Keys)
        {
            if (!Bot.HasPermissionsInGuild(guildId, Permissions.ManageGuild))
            {
                var guildConfig = await db.GetOrCreateGuildConfigAsync(guildId);
                if (guildConfig.HasSetting(GuildSettings.FilterDiscordInvites))
                {
                    Logger.LogWarning("Guild {GuildId} has FilterDiscordInvites enabled but the bot cannot fetch invites.",
                        guildId.RawValue);
                }

                continue;
            }

            var inviteCodes = new HashSet<string>();

            try
            {
                var invites = await Bot.FetchGuildInvitesAsync(guildId, cancellationToken: stoppingToken);
                foreach (var invite in invites)
                {
                    inviteCodes.Add(invite.Code);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to fetch invites in guild {GuildId}.",
                    guildId.RawValue);
            }

            var vanityInviteCode = Bot.GetGuild(guildId)?.VanityUrlCode;
            if (!string.IsNullOrWhiteSpace(vanityInviteCode))
                inviteCodes.Add(vanityInviteCode);

            _guildInviteCodes[guildId] = inviteCodes;
        }
    }

    protected override ValueTask OnInviteCreated(InviteCreatedEventArgs e)
    {
        if (!e.GuildId.HasValue)
            return ValueTask.CompletedTask;

        if (_guildInviteCodes.TryGetValue(e.GuildId.Value, out var inviteCodes))
        {
            inviteCodes.Add(e.Code);
        }
        else
        {
            _guildInviteCodes[e.GuildId.Value] = new[] {e.Code}.ToHashSet();
        }

        return ValueTask.CompletedTask;
    }

    protected override ValueTask OnInviteDeleted(InviteDeletedEventArgs e)
    {
        if (!e.GuildId.HasValue)
            return ValueTask.CompletedTask;

        if (_guildInviteCodes.TryGetValue(e.GuildId.Value, out var inviteCodes))
        {
            inviteCodes.Remove(e.Code);
        }
        else
        {
            _guildInviteCodes[e.GuildId.Value] = new HashSet<string>(); // just make it an empty hashset
        }

        return ValueTask.CompletedTask;
    }

    protected override async ValueTask OnMessageReceived(BotMessageReceivedEventArgs e)
    {
        if (e.GuildId is not { } guildId || string.IsNullOrWhiteSpace(e.Message.Content))
            return;

        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        var guild = await db.GetOrCreateGuildConfigAsync(guildId);
        if (!guild.HasSetting(GuildSettings.FilterDiscordInvites))
            return;

        await db.Guilds.Entry(guild)
            .Collection(x => x.InviteFilterExemptions!)
            .LoadAsync();

        if (!InviteRegex.IsMatch(e.Message.Content, out var match))
            return;

        var inviteCode = match.Groups[0].Value;
        if (_guildInviteCodes.TryGetValue(guildId, out var inviteCodes) && inviteCodes.Contains(inviteCode))
            return;

        foreach (var exemption in guild.InviteFilterExemptions!)
        {
            switch (exemption.ExemptionType)
            {
                case InviteFilterExemptionType.Guild:
                {
                    try
                    {
                        var invite = await Bot.FetchInviteAsync(inviteCode);
                        if (invite is IGuildInvite guildInvite && guildInvite.GuildId == exemption.GuildId)
                            return;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "Failed to fetch invite with code {Code}.", inviteCode);
                    }
                    break;
                }
                case InviteFilterExemptionType.Role:
                {
                    var member = e.Member ?? await Bot.GetOrFetchMemberAsync(e.GuildId.Value, e.AuthorId);
                    if (member?.RoleIds.Contains(exemption.TargetId!.Value) == true)
                    {
                        return;
                    }
                    break;
                }
                case InviteFilterExemptionType.User when e.AuthorId == exemption.TargetId:
                case InviteFilterExemptionType.Channel when e.ChannelId == exemption.TargetId:
                case InviteFilterExemptionType.Channel when Bot.GetChannel(guildId, e.ChannelId) is IThreadChannel thread && thread.ChannelId == exemption.TargetId:
                case InviteFilterExemptionType.InviteCode when exemption.InviteCode!.Equals(inviteCode):
                    return;
                default:
                    continue;
            }
        }

        try
        {
            await e.Message.DeleteAsync(new DefaultRestRequestOptions().WithReason("Message was filtered by automatic invite filter."));
            DeletedMessageIds.Add(e.MessageId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete message {MessageId} with invite {InviteCode} in guild {GuildId} that triggered the automatic invite filter.",
                e.MessageId.RawValue, inviteCode, e.GuildId.Value.RawValue);
        }
    }
}