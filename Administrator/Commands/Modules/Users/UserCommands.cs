using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Extensions;
using Administrator.Services;
using Discord;
using Discord.WebSocket;
using Humanizer.Localisation;
using Qmmands;
using Color = Discord.Color;

namespace Administrator.Commands.Modules.Users
{
    [Name("Users")]
    [Group("user")]
    public class UserCommands : AdminModuleBase
    {
        public ConfigurationService Config { get; set; }

        public PaginationService Pagination { get; set; }

        public LevelService Levels { get; set; }

        public HttpClient Http { get; set; }

        [Command("", "info")]
        [RequireContext(ContextType.Guild)]
        public AdminCommandResult GetGuildUserInfo([Remainder] SocketGuildUser target = null)
            => GetUserInfo(target ?? (SocketGuildUser) Context.User);

        [Command("", "info")]
        public async ValueTask<AdminCommandResult> GetUserInfoAsync(ulong targetId)
        {
            var target = await Context.Client.Rest.GetUserAsync(targetId);
            return target is { }
                ? GetUserInfo(target)
                : CommandErrorLocalized("userparser_notfound");
        }

        private AdminCommandResult GetUserInfo(IUser target)
        {
            var builder = new EmbedBuilder()
                .WithTitle(Localize(target.IsBot ? "user_info_title_bot" : "user_info_title",
                    target.ToString().Sanitize()))
                .AddField(Localize("info_id"), target.Id, true)
                .AddField(Localize("info_mention"), target.Mention, true)
                .WithThumbnailUrl(target.GetAvatarOrDefault(size: 256));

            var guildTarget = target as SocketGuildUser;
            var isGuildTarget = guildTarget is { };

            if (isGuildTarget)
            {
                var highestRole = guildTarget.GetHighestRole(x => x.Color != Color.Default);
                builder.WithColor(highestRole?.Color ?? Config.SuccessColor);

                if (!string.IsNullOrWhiteSpace(guildTarget.Nickname))
                {
                    builder.AddField(Localize("user_info_nickname"), guildTarget.Nickname.Sanitize(), true);
                }
            }
            else
            {
                builder.WithColor(Config.SuccessColor);
            }

            builder.AddField(Localize("user_info_status"), Localize(target.Status switch
            {
                UserStatus.Online => "user_info_online",
                UserStatus.Idle => "user_info_idle",
                UserStatus.AFK => "user_info_idle",
                UserStatus.DoNotDisturb => "user_info_dnd",
                UserStatus.Invisible => "user_info_offline",
                UserStatus.Offline => "user_info_offline",
                _ => throw new ArgumentOutOfRangeException()
            }), true);

            builder.AddField(Localize("user_info_created"), string.Join('\n', target.CreatedAt.ToString("g", Context.Language.Culture),
                (DateTimeOffset.UtcNow - target.CreatedAt).HumanizeFormatted(Context, TimeUnit.Second, true)), true);

            if (isGuildTarget)
            {
                var joinedAt = guildTarget.JoinedAt ?? DateTimeOffset.UtcNow;
                builder.AddField(Localize("user_info_joined", Context.Guild.Name), string.Join('\n',
                    joinedAt.ToString("g", Context.Language.Culture),
                    (DateTimeOffset.UtcNow - joinedAt).HumanizeFormatted(Context, TimeUnit.Second, true)), true);

                var roles = guildTarget.Roles.Where(x => !x.IsEveryone).OrderByDescending(x => x.Position).ToList();
                if (roles.Count > 0)
                {
                    builder.AddField(Localize("user_info_roles", roles.Count),
                        string.Join(", ", roles.Select(x => x.Name.Sanitize())).TrimTo(1024, true));
                }
            }

            if (target.Activity is { } activity)
            {
                switch (activity)
                {
                    case RichGame richGame:
                        var status = string.IsNullOrWhiteSpace(richGame.Details)
                            ? richGame.Name
                            : $"{richGame.Name} ({richGame.Details})";
                        builder.WithFooter(Localize("user_info_playing", status), richGame.LargeAsset.GetImageUrl());
                        break;
                    case SpotifyGame spotify:
                        builder.WithFooter(Localize("user_info_listening", spotify.TrackTitle, spotify.Artists.First()),
                            spotify.AlbumArtUrl);
                        break;
                    case StreamingGame stream:
                        builder.WithFooter(Localize("user_info_streaming", stream.Name, stream.Details),
                            "https://i.imgur.com/e6JdpuP.png"); // twitch icon
                        break;
                    case Game game:
                        status = string.IsNullOrWhiteSpace(game.Details)
                            ? game.Name
                            : $"{game.Name} ({game.Details})";
                        builder.WithFooter(Localize("user_info_playing", status));
                        break;
                }
            }

            return CommandSuccess(embed: builder.Build());
        }

        [RequireContext(ContextType.Guild)]
        public class GuildUserCommands : UserCommands
        {
            [Group("nickname", "nick")]
            public sealed class NicknameCommands : UserCommands
            {
                [Command]
                public AdminCommandResult GetNickname([Remainder] SocketGuildUser target)
                {
                    return string.IsNullOrWhiteSpace(target.Nickname)
                        ? CommandSuccessLocalized("user_no_nickname", args: Format.Bold(target.ToString().Sanitize()))
                        : CommandSuccessLocalized("user_nickname",
                            args: new object[] { Format.Bold(target.ToString().Sanitize()), Format.Bold(target.Nickname.Sanitize()) });
                }

                [Command("set")]
                [RequireUserPermissions(GuildPermission.ManageNicknames)]
                [RequireBotPermissions(GuildPermission.ManageNicknames)]
                public async ValueTask<AdminCommandResult> SetNicknameAsync([RequireHierarchy] SocketGuildUser target,
                    [Remainder, MustBe(StringLength.ShorterThan, 33)] string newNickname)
                {
                    await target.ModifyAsync(x => x.Nickname = newNickname);
                    return CommandSuccessLocalized("user_nickname_updated");
                }

                [Command("set")]
                [RequireBotPermissions(GuildPermission.ManageNicknames)]
                public async ValueTask<AdminCommandResult> SetNicknameAsync(
                    [Remainder, MustBe(StringLength.ShorterThan, 33)] string newNickname)
                {
                    var target = (SocketGuildUser) Context.User;
                    if (Context.Guild.CurrentUser.Hierarchy <= target.Hierarchy)
                        return CommandErrorLocalized("requirehierarchy_self");

                    await target.ModifyAsync(x => x.Nickname = newNickname);
                    return CommandSuccessLocalized("user_nickname_updated");
                }

                [Command("reset")]
                [RequireBotPermissions(GuildPermission.ManageNicknames)]
                public async ValueTask<AdminCommandResult> SetNicknameAsync([RequireHierarchy] SocketGuildUser target)
                {
                    await target.ModifyAsync(x => x.Nickname = string.Empty);
                    return CommandSuccessLocalized("user_nickname_reset");
                }

                [Command("reset")]
                [RequireBotPermissions(GuildPermission.ManageNicknames)]
                public async ValueTask<AdminCommandResult> SetNicknameAsync()
                {
                    var target = (SocketGuildUser)Context.User;
                    if (Context.Guild.CurrentUser.Hierarchy <= target.Hierarchy)
                        return CommandErrorLocalized("requirehierarchy_self");

                    await target.ModifyAsync(x => x.Nickname = string.Empty);
                    return CommandSuccessLocalized("user_nickname_reset");
                }
            }

            [Command("search"), RunMode(RunMode.Parallel)]
            [RequireUserPermissions(ChannelPermission.ManageMessages, Group = "user")]
            [RequireUserPermissions(ChannelPermission.ManageMessages, Group = "user")]
            public async ValueTask<AdminCommandResult> SearchUsersAsync(
                [Remainder, MustBe(StringLength.ShorterThan, 33)] string input)
            {
                // input = input.ToLower();
                var matches = new List<ValueTuple<int, SocketGuildUser>>();
                foreach (var user in Context.Guild.Users.Where(x => !string.IsNullOrWhiteSpace(x.Username)))
                {
                    var nickDistance = int.MaxValue;
                    if (!string.IsNullOrWhiteSpace(user.Nickname))
                    {
                        nickDistance = input.GetLevenshteinDistanceTo(user.Nickname.ToLower());
                        if (user.Nickname.Contains(input, StringComparison.OrdinalIgnoreCase))
                            nickDistance -= 6;
                    }
                    
                    var nameDistance = input.GetLevenshteinDistanceTo(user.Username.ToLower());
                    if (user.Username.Contains(input, StringComparison.OrdinalIgnoreCase))
                        nameDistance -= 8; // higher weighting for usernames

                    var distance = Math.Min(nameDistance, nickDistance);
                    if (distance <= 2)
                        matches.Add((distance, user));
                }

                if (matches.Count == 0)
                    return CommandErrorLocalized("user_search_no_results", args: input);

                matches = matches.OrderBy(x => x.Item1).ToList();

                var pages = DefaultPaginator.GeneratePages(matches, 1024, x => FormatUser(x.Item2),
                    embedFunc: builder =>
                        builder.WithSuccessColor().WithTitle(Localize("user_search_results", input)));

                var message = await Pagination.SendPaginatorAsync(Context.Channel, pages[0]);
                await using var paginator = new DefaultPaginator(message, pages, 0, Pagination);
                await paginator.WaitForExpiryAsync();
                return CommandSuccess();
            }

            [Command("searchregex"), RunMode(RunMode.Parallel)]
            [RequireUserPermissions(ChannelPermission.ManageMessages, Group = "user")]
            [RequireUserPermissions(ChannelPermission.ManageMessages, Group = "user")]
            public async ValueTask<AdminCommandResult> RegexSearchUsersAsync([Remainder] Regex regex)
            {
                var matches = new List<SocketGuildUser>();
                var delay = Task.Delay(TimeSpan.FromSeconds(5));
                var task = Task.Run(() =>
                {
                    matches = Context.Guild.Users.Where(MatchesRegex)
                        .OrderByDescending(x => x.JoinedAt ?? DateTimeOffset.UtcNow)
                        .ToList();
                });

                using var _ = Context.Channel.EnterTypingState();
                var timeoutTask = await Task.WhenAny(delay, task);
                if (timeoutTask == delay)
                    return CommandErrorLocalized("user_searchregex_timeout");
                
                if (matches.Count == 0)
                    return CommandErrorLocalized("user_searchregex_no_results", args: regex.ToString());

                var pages = DefaultPaginator.GeneratePages(matches, 1024, FormatUser, embedFunc: builder =>
                        builder.WithSuccessColor().WithTitle(Localize("user_searchregex_results")));

                var message = await Pagination.SendPaginatorAsync(Context.Channel, pages[0]);
                await using var paginator = new DefaultPaginator(message, pages, 0, Pagination);
                await paginator.WaitForExpiryAsync();
                return CommandSuccess();

                bool MatchesRegex(SocketGuildUser target)
                {
                    if (string.IsNullOrWhiteSpace(target.Username)) return false;

                    if (string.IsNullOrWhiteSpace(target.Nickname))
                        return regex.IsMatch(target.Username);

                    return regex.IsMatch(target.Nickname) || regex.IsMatch(target.Username);
                }
            }

            private string FormatUser(SocketGuildUser target)
            {
                return string.IsNullOrWhiteSpace(target.Nickname)
                    ? target.Format()
                    : Localize("user_search_format", Format.Bold(target.ToString().Sanitize()),
                        Format.Bold(target.Nickname.Sanitize()),
                        Format.Code(target.Id.ToString()));
            }
        }

        [Command("xp"), RunMode(RunMode.Parallel)]
        public async ValueTask<AdminCommandResult> GetUserXpAsync([Remainder] SocketGuildUser target = null)
        {
            using var _ = Context.Channel.EnterTypingState();
            var image = await Levels.CreateXpImageAsync(Context, target ?? Context.User);

            return CommandSuccess(file: new MessageFile(image, "xp.png"));
        }

        [Command("language")]
        public async ValueTask<AdminCommandResult> GetLanguageAsync()
        {
            var user = await Context.Database.GetOrCreateGlobalUserAsync(Context.User.Id);
            return CommandSuccessLocalized("user_language", args:
                $"{Format.Bold(user.Language.NativeName)} ({user.Language.EnglishName}, `{user.Language.CultureCode}`)");
        }

        [Command("language")]
        public async ValueTask<AdminCommandResult> SetLanguageAsync([Remainder] LocalizedLanguage newLanguage)
        {
            var user = await Context.Database.GetOrCreateGlobalUserAsync(Context.User.Id);
            user.Language = newLanguage;
            Context.Database.GlobalUsers.Update(user);
            await Context.Database.SaveChangesAsync();
            Context.Language = newLanguage;

            return CommandSuccessLocalized("user_language_set", args:
                $"{Format.Bold(user.Language.NativeName)} ({user.Language.EnglishName}, `{user.Language.CultureCode}`)");
        }

        [Command("languages")]
        public AdminCommandResult GetLanguages()
            => CommandSuccess(new StringBuilder()
                .AppendLine(Localize("available_languages"))
                .AppendJoin('\n',
                    Localization.Languages.Select(
                        x => Format.Code($"{x.NativeName} ({x.EnglishName}, {x.CultureCode})"))).ToString());
    }
}