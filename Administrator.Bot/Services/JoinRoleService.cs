using Administrator.Database;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Disqord.Rest;

namespace Administrator.Bot;

public sealed class JoinRoleService : DiscordBotService
{
    protected override async ValueTask OnMemberJoined(MemberJoinedEventArgs e)
    {
        if (e.Member.IsPending)
            return;

        await GrantJoinRoleAsync(e.GuildId, e.MemberId);
    }

    protected override async ValueTask OnMemberUpdated(MemberUpdatedEventArgs e)
    {
        if (e.OldMember?.IsPending != false)
            return;

        if (e.OldMember.IsPending == e.NewMember.IsPending)
            return;

        await GrantJoinRoleAsync(e.GuildId, e.MemberId);
    }

    private async Task GrantJoinRoleAsync(Snowflake guildId, Snowflake memberId)
    {
        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        var guild = await db.Guilds.GetOrCreateAsync(guildId);

        if (guild.JoinRoleId is not { } roleId)
            return;

        try
        {
            await Bot.GrantRoleAsync(guildId, memberId, roleId);
        }
        catch (RestApiException ex) when (ex.ErrorModel?.Code == RestApiErrorCode.UnknownRole)
        {
            await Bot.TrySendErrorAsync(guildId,
                $"The server's join role (ID {Markdown.Code(roleId)}) could not be found and was likely deleted, so it has been unset as the join role.");
        }
        catch (RestApiException ex) when (ex.ErrorModel?.Code == RestApiErrorCode.MissingPermissions)
        {
            await Bot.TrySendErrorAsync(guildId,
                $"The server's join role ({Mention.Role(roleId)}) could not be assigned to the newly joined member {Mention.User(memberId)} due to incorrectly configured permissions.");
        }
        catch (Exception ex)
        {
            await Bot.TrySendErrorAsync(guildId,
                $"The server's join role ({Mention.Role(roleId)}) could not be assigned to the newly joined member {Mention.User(memberId)} due to an unhandled error:\n" +
                Markdown.CodeBlock(ex.Message));
        }
    }
}