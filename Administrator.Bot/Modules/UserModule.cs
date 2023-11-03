using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Extensions.Interactivity.Menus.Paged;
using Disqord.Gateway;
using Microsoft.EntityFrameworkCore;
using Qmmands;
using Qommon;

namespace Administrator.Bot;

[SlashGroup("user")]
public sealed class UserModule(AttachmentService attachments) : DiscordApplicationGuildModuleBase
{
    [SlashCommand("info")]
    [Description("Displays information for a user or member.")]
    public async Task<IResult> DisplayInfoAsync(
        [Description("The user/member to display information for. Defaults to yourself.")]
            IUser? user = null)
    {
        await Deferral();
        user ??= Context.Author;

        var embed = new LocalEmbed()
            .WithUnusualColor()
            .WithThumbnailUrl(user.GetAvatarUrl(CdnAssetFormat.Automatic, 512))
            .WithTitle($"Information for user {user.Tag}")
            .AddField("ID", user.Id, true)
            .AddField("Mention", user.Mention);

        if (user is IMember member)
        {
            embed.WithThumbnailUrl(member.GetGuildAvatarUrl(CdnAssetFormat.Automatic, 512));

            if (!string.IsNullOrWhiteSpace(member.Nick))
                embed.AddField("Nickname", member.Nick);

            embed.AddField("Last joined server",
                Markdown.Timestamp(member.JoinedAt.GetValueOrNullable() ?? DateTimeOffset.UtcNow,
                    Markdown.TimestampFormat.RelativeTime));

            if (member.GetHighestRole(x => x.Color.HasValue) is { } role)
                embed.WithColor(role.Color!.Value);

            if (member.GetPresence() is { } presence)
            {
                embed.AddField("Status", presence.Status);

                if (presence.Activities.FirstOrDefault(x => x is not ICustomActivity) is { } activity)
                {
                    var footerText = activity switch
                    {
                        IRichActivity richActivity => !string.IsNullOrWhiteSpace(richActivity.Details)
                            ? $"{richActivity.Name} ({richActivity.Details})"
                            : richActivity.Name,
                        ISpotifyActivity spotifyActivity => $"Listening to \"{spotifyActivity.TrackTitle}\" by {string.Join(", ", spotifyActivity.Artists)} on Spotify",
                        IStreamingActivity streamingActivity => $"Streaming now: \"{streamingActivity.Name}\" - {streamingActivity.Url}",
                        _ => null
                    };

                    if (!string.IsNullOrWhiteSpace(footerText))
                        embed.WithFooter(footerText.Truncate(Discord.Limits.Message.Embed.Footer.MaxTextLength));
                }
            }
        }

        embed.AddField("Account created",
            Markdown.Timestamp(user.CreatedAt(), Markdown.TimestampFormat.RelativeTime));

        return Response(embed);
    }

    [SlashCommand("avatar")]
    [Description("Displays a user or member's avatar.")]
    public async Task<IResult> DisplayAvatarAsync(
        [Description("The user/member whose avatar is being displayed. Defaults to yourself.")]
            IUser? user = null,
        [Name("server-avatar")]
        [Description("Whether to display a server avatar (if set), or a global avatar. Default: True")]
            bool guildAvatar = true)
    {
        await Deferral();
        user ??= Context.Author;

        var avatarUrl = guildAvatar
            ? (user as IMember)?.GetGuildAvatarUrl(CdnAssetFormat.Automatic, 1024) ?? user.GetAvatarUrl(CdnAssetFormat.Automatic, 1024)
            : user.GetAvatarUrl(CdnAssetFormat.Automatic, 1024);

        var avatarType = guildAvatar && !string.IsNullOrWhiteSpace((user as IMember)?.GuildAvatarHash)
            ? "server"
            : "global";

        var target = user.Id == Context.AuthorId
            ? "Your"
            : $"{user.Mention}'s";

        var (stream, fileName) = await attachments.GetAttachmentAsync(avatarUrl);
        return Response(new LocalInteractionMessageResponse()
            .WithContent($"{target} {avatarType} avatar:")
            .AddAttachment(new LocalAttachment(stream, fileName))
            .WithAllowedMentions(LocalAllowedMentions.None));
    }

    [SlashGroup("xp")]
    public sealed class UserXpModule : DiscordApplicationGuildModuleBase
    {
        private readonly AdminDbContext _db;
        private readonly XpService _xp;

        public UserXpModule(AdminDbContext db, XpService xp)
        {
            _db = db;
            _xp = xp;
        }

        [SlashCommand("stats")]
        [Description("Displays global and server XP statistics for a user or member.")]
        public async Task<IResult> DisplayStatsAsync(
            [Description("The user/member to display statistics for. Defaults to yourself.")]
                IUser? user = null)
        {
            await Deferral();
            user ??= Context.Author;

            if (user is not IMember member)
                return Response("Failed to fetch XP stats: The supplied user is not a member of this server!").AsEphemeral();

            var guildUser = await _db.GetOrCreateGuildUserAsync(member.GuildId, member.Id);
            var globalUser = await _db.GetOrCreateGlobalUserAsync(member.Id);

            return Response(new LocalEmbed()
                    .WithUnusualColor()
                    .WithThumbnailUrl(_xp.GetLevelEmoji(guildUser.Tier, guildUser.Level).GetUrl())
                    .WithAuthor($"Server XP stats for {member.GetDisplayName()}", Bot.GetGuild(Context.GuildId)?.GetIconUrl() ?? member.GetGuildAvatarUrl())
                    .WithDescription(
                        $"Tier {guildUser.Tier}, Level {guildUser.Level} + {guildUser.CurrentLevelXp}/{guildUser.NextLevelXp}")
                    .WithFooter($"\"{guildUser.Blurb}\"", member.GetGuildAvatarUrl()),
                new LocalEmbed()
                    .WithUnusualColor()
                    .WithThumbnailUrl(_xp.GetLevelEmoji(globalUser.Tier, globalUser.Level).GetUrl())
                    .WithAuthor($"Global XP stats for {member.GetDisplayName()}", member.GetAvatarUrl())
                    .WithDescription(
                        $"Tier {globalUser.Tier}, Level {globalUser.Level} + {globalUser.CurrentLevelXp}/{globalUser.NextLevelXp}"));
        }

        [SlashCommand("leaderboard")]
        [Description("Displays an XP leaderboard for this server.")]
        public async Task<IResult> DisplayLeaderboardAsync(
            [Description("Whether to start on the page your rank is on. Default: False")]
                bool startWithSelf = false)
        {
            await Deferral();
            var guildXpStats = await _db.GuildUsers.Where(x => x.GuildId == Context.GuildId)
                .OrderByDescending(x => x.TotalXp)
                .ToListAsync();

            var pages = new List<Page>();
            var startingPageIndex = 0;
            var index = 0;
            var position = 0;
            foreach (var chunk in guildXpStats.Chunk(10))
            {
                var embed = new LocalEmbed()
                    .WithUnusualColor()
                    .WithTitle($"{Bot.GetGuild(Context.GuildId)!.Name} XP leaderboard");

                foreach (var guildUser in chunk)
                {
                    if (startWithSelf && guildUser.UserId == Context.AuthorId)
                        startingPageIndex = index;

                    var name = Bot.GetMember(guildUser.GuildId, guildUser.UserId)?.Tag
                               ?? guildUser.UserId.ToString();

                    var emoji = _xp.GetLevelEmoji(guildUser.Tier, guildUser.Level);

                    embed.AddField($"{++position}. {emoji} {name}",
                        $"Tier {guildUser.Tier}, Level {guildUser.Level} + {guildUser.CurrentLevelXp}/{guildUser.NextLevelXp}");
                }

                pages.Add(new Page().AddEmbed(embed));

                index++;
            }

            return View(new PagedView(new ListPageProvider(pages)) {CurrentPageIndex = startingPageIndex});
        }

        [SlashCommand("blurb")]
        [Description("Updates your personal \"blurb\" text for this server.")]
        public async Task<IResult> UpdateBlurbAsync(
            [Description("The new text to set. Will only be displayed in this server.")] 
            [Range(1, 100)]
                string text)
        {
            var guildUser = await _db.GetOrCreateGuildUserAsync(Context.GuildId, Context.AuthorId);
            guildUser.Blurb = text;
            await _db.SaveChangesAsync();

            return Response($"Your blurb in this server has been updated to \"{text}\".");
        }
    }
}