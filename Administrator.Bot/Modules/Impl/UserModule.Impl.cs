using System.Text;
using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Extensions.Interactivity.Menus.Paged;
using Disqord.Gateway;
using Microsoft.EntityFrameworkCore;
using Qmmands;
using Qommon;

namespace Administrator.Bot;

public sealed partial class UserModule(AttachmentService attachments) : DiscordApplicationModuleBase
{
    public partial async Task<IResult> Info(IUser? user)
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
            embed.WithDescription(new StringBuilder($"{Markdown.Bold($"Roles ({member.RoleIds.Count(x => x != Context.GuildId)}):")}\n")
                .AppendJoinTruncated(", ", member.GetRoles().Values.OrderByDescending(x => x.Position).Select(x => x.Id)
                        .Except([Context.GuildId.GetValueOrDefault()]).Select(Mention.Role),
                    Discord.Limits.Message.Embed.MaxDescriptionLength).ToString());
            
            embed.WithThumbnailUrl(member.GetGuildAvatarUrl(CdnAssetFormat.Automatic, 512));

            if (!string.IsNullOrWhiteSpace(member.Nick))
                embed.AddField("Nickname", member.Nick);

            embed.AddField("Last joined server",
                Markdown.Timestamp(member.JoinedAt.GetValueOrNullable() ?? DateTimeOffset.UtcNow,
                    Markdown.TimestampFormat.RelativeTime));

            if (member.GetHighestRole(x => x.Color.HasValue) is { } role)
                embed.WithColor(role.Color!.Value);

            /*
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
            */
        }

        embed.AddField("Account created",
            Markdown.Timestamp(user.CreatedAt(), Markdown.TimestampFormat.RelativeTime));

        return Response(embed);
    }

    public partial async Task<IResult> ServerAvatar(IMember? member)
    {
        member ??= (IMember) Context.Author;

        var avatarUrl = member.GetGuildAvatarUrl(CdnAssetFormat.Automatic, 1024);

        var target = member.Id == Context.AuthorId
            ? "Your"
            : $"{member.Mention}'s";

        var (stream, fileName) = await attachments.GetAttachmentAsync(avatarUrl);
        return Response(new LocalInteractionMessageResponse()
            .WithContent($"{target} server avatar:")
            .AddAttachment(new LocalAttachment(stream, fileName))
            .WithAllowedMentions(LocalAllowedMentions.None));
    }

    public partial async Task<IResult> Avatar(IUser? user)
    {
        await Deferral();
        user ??= Context.Author;

        var target = user.Id == Context.AuthorId
            ? "Your"
            : $"{user.Mention}'s";

        var (stream, fileName) = await attachments.GetAttachmentAsync(user.GetAvatarUrl(CdnAssetFormat.Automatic, size: 1024));
        return Response(new LocalInteractionMessageResponse()
            .WithContent($"{target} global avatar:")
            .AddAttachment(new LocalAttachment(stream, fileName))
            .WithAllowedMentions(LocalAllowedMentions.None));
    }

    public sealed partial class UserXpModule(ImageService images, AdminDbContext db, EmojiService emojis) : DiscordApplicationModuleBase
    {
        public partial async Task<IResult> Stats(IUser? user)
        {
            await Deferral();
            user ??= Context.Author;

            var result = await images.GenerateXpImageAsync(Context.GuildId, user.Id);
            if (!result.IsSuccessful)
                return Response(result.ErrorMessage);
            
            return Response(new LocalInteractionMessageResponse().AddAttachment(result.Value));

            /*
            if (Context.GuildId.HasValue && user is not IMember member)
                return Response("Failed to fetch XP stats: The supplied user is not a member of this server!").AsEphemeral();

            var guildUser = await _db.Members.GetOrCreateAsync(member.GuildId, member.Id);
            var globalUser = await _db.Users.GetOrCreateAsync(member.Id);

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
            */
        }

        public partial async Task<IResult> Leaderboard(bool startWithSelf)
        {
            await Deferral();
            var guildXpStats = await db.Members.Where(x => x.GuildId == Context.GuildId)
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
                    .WithTitle($"{Bot.GetGuild(Context.GuildId!.Value)!.Name} XP leaderboard");

                foreach (var guildUser in chunk)
                {
                    if (startWithSelf && guildUser.UserId == Context.AuthorId)
                        startingPageIndex = index;

                    var name = Bot.GetMember(guildUser.GuildId, guildUser.UserId)?.Tag
                               ?? guildUser.UserId.ToString();

                    var emoji = emojis.GetLevelEmoji(guildUser.GetTier(), guildUser.GetLevel());

                    embed.AddField($"{++position}. {emoji} {name}",
                        $"Tier {guildUser.GetTier()}, Level {guildUser.GetLevel()} + {guildUser.GetCurrentLevelXp()}/{guildUser.GetNextLevelXp()}");
                }

                pages.Add(new Page().AddEmbed(embed));

                index++;
            }

            return Menu(new AdminInteractionMenu(new AdminPagedView(pages) { CurrentPageIndex = startingPageIndex }, Context.Interaction)
                { AuthorId = Context.AuthorId });
        }

        public partial async Task<IResult> Blurb(string text)
        {
            var guildUser = await db.Members.GetOrCreateAsync(Context.GuildId!.Value, Context.AuthorId);
            guildUser.Blurb = text;
            await db.SaveChangesAsync();

            return Response($"Your blurb in this server has been updated to \"{text}\".");
        }
    }
}