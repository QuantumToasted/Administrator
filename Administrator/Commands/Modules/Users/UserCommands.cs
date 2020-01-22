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
using Disqord;
using Humanizer.Localisation;
using Qmmands;

namespace Administrator.Commands
{
    [Name("Users")]
    [Group("user")]
    public class UserCommands : AdminModuleBase
    {
        public ConfigurationService Config { get; set; }

        public PaginationService Pagination { get; set; }

        public LevelService Levels { get; set; }

        public HttpClient Http { get; set; }

        public CommandHandlerService CommandHandler { get; set; }

        [Command("", "info")]
        [RequireContext(ContextType.Guild)]
        public AdminCommandResult GetGuildUserInfo([Remainder] CachedMember target = null)
            => GetUserInfo(target ?? (CachedMember) Context.User);

        [Command("", "info")]
        public async ValueTask<AdminCommandResult> GetUserInfoAsync(ulong targetId)
        {
            var target = await Context.Client.GetUserAsync(targetId);
            return target is { }
                ? GetUserInfo(target)
                : CommandErrorLocalized("userparser_notfound");
        }

        private AdminCommandResult GetUserInfo(IUser target)
        {
            var builder = new LocalEmbedBuilder()
                .WithTitle(Localize(target.IsBot ? "user_info_title_bot" : "user_info_title",
                    target.Tag.Sanitize()))
                .AddField(Localize("info_id"), target.Id, true)
                .AddField(Localize("info_mention"), target.Mention, true)
                .WithThumbnailUrl(target.GetAvatarUrl(size: 256));

            var guildTarget = target as CachedMember;
            var isGuildTarget = guildTarget is { };

            if (isGuildTarget)
            {
                var highestRole = guildTarget.GetHighestRole(x => x.Color.HasValue);
                builder.WithColor(highestRole?.Color ?? Config.SuccessColor);

                if (!string.IsNullOrWhiteSpace(guildTarget.Nick))
                {
                    builder.AddField(Localize("user_info_nickname"), guildTarget.Nick.Sanitize(), true);
                }
            }
            else
            {
                builder.WithColor(Config.SuccessColor);
            }

            builder.AddField(Localize("user_info_status"), Localize(guildTarget?.Presence?.Status switch
            {
                UserStatus.Online => "user_info_online",
                UserStatus.Idle => "user_info_idle",
                UserStatus.DoNotDisturb => "user_info_dnd",
                _ => "user_info_offline"
            }), true);

            builder.AddField(Localize("user_info_created"), string.Join('\n', target.Id.CreatedAt.ToString("g", Context.Language.Culture),
                (DateTimeOffset.UtcNow - target.Id.CreatedAt).HumanizeFormatted(Localization, Context.Language, TimeUnit.Second, true)), true);

            if (isGuildTarget)
            {
                builder.AddField(Localize("user_info_joined", Context.Guild.Name), string.Join('\n',
                    guildTarget.JoinedAt.ToString("g", Context.Language.Culture),
                    (DateTimeOffset.UtcNow - guildTarget.JoinedAt).HumanizeFormatted(Localization, Context.Language, TimeUnit.Second, true)), true);

                var roles = guildTarget.Roles.Values.Where(x => !x.IsDefault).OrderByDescending(x => x.Position).ToList();
                if (roles.Count > 0)
                {
                    builder.AddField(Localize("user_info_roles", roles.Count),
                        string.Join(", ", roles.Select(x => x.Name.Sanitize())).TrimTo(1024, true));
                }
            }

            switch (guildTarget?.Presence?.Activity)
            {
                case CustomActivity customActivity:
                    var status = string.IsNullOrWhiteSpace(customActivity.Text)
                        ? customActivity.Name
                        : $"{customActivity.Name} ({customActivity.Text})";
                    builder.WithFooter(Localize("user_info_playing", status));
                    break;
                case RichActivity richActivity:
                    status = string.IsNullOrWhiteSpace(richActivity.Details)
                        ? richActivity.Name
                        : $"{richActivity.Name} ({richActivity.Details})";
                    builder.WithFooter(Localize("user_info_playing", status), richActivity.LargeAsset?.Url);
                    break;
                case SpotifyActivity spotifyActivity:
                    builder.WithFooter(Localize("user_info_listening", spotifyActivity.TrackTitle, spotifyActivity.Artists.First()),
                        spotifyActivity.AlbumCoverUrl);
                    break;
                case StreamingActivity streamingActivity:
                    builder.WithFooter(Localize("user_info_streaming", streamingActivity.Name, streamingActivity.Url));
                    break;
            }

            return CommandSuccess(embed: builder.Build());
        }

        [Command("avatar", "av"), RunMode(RunMode.Parallel)]
        [RequireContext(ContextType.Guild)]
        public async ValueTask<AdminCommandResult> GetUserAvatarAsync([Remainder] CachedUser target = null)
        {
            var user = await Context.Client.GetUserAsync((target ?? Context.User).Id);
            using var _ = Context.Channel.Typing();
            var avatarUrl = new Uri(user.GetAvatarUrl());
            var stream = await Http.GetStreamAsync(avatarUrl);
            return CommandSuccessLocalized("user_avatar", attachment: new LocalAttachment(stream, avatarUrl.LocalPath),
                args: Markdown.Bold(user.Tag.Sanitize()));
        }

        [RequireContext(ContextType.Guild)]
        public class GuildUserCommands : UserCommands
        {
            [Group("nickname", "nick")]
            public sealed class NicknameCommands : UserCommands
            {
                [Command]
                public AdminCommandResult GetNickname([Remainder] CachedMember target)
                {
                    return string.IsNullOrWhiteSpace(target.Nick)
                        ? CommandSuccessLocalized("user_no_nickname", args: Markdown.Bold(target.Tag.Sanitize()))
                        : CommandSuccessLocalized("user_nickname",
                            args: new object[] { Markdown.Bold(target.Tag.Sanitize()), Markdown.Bold(target.Nick.Sanitize()) });
                }

                [Command("set")]
                [RequireUserPermissions(Permission.ManageNicknames)]
                [RequireBotPermissions(Permission.ManageNicknames)]
                public async ValueTask<AdminCommandResult> SetNicknameAsync([RequireHierarchy] CachedMember target,
                    [Remainder, MustBe(StringLength.ShorterThan, 33)] string newNickname)
                {
                    await target.ModifyAsync(x => x.Nick = newNickname);
                    return CommandSuccessLocalized("user_nickname_updated");
                }

                [Command("set")]
                [RequireBotPermissions(Permission.ManageNicknames)]
                public async ValueTask<AdminCommandResult> SetNicknameAsync(
                    [Remainder, MustBe(StringLength.ShorterThan, 33)] string newNickname)
                {
                    var target = (CachedMember) Context.User;
                    if (Context.Guild.CurrentMember.Hierarchy <= target.Hierarchy)
                        return CommandErrorLocalized("requirehierarchy_self");

                    await target.ModifyAsync(x => x.Nick = newNickname);
                    return CommandSuccessLocalized("user_nickname_updated");
                }

                [Command("reset")]
                [RequireBotPermissions(Permission.ManageNicknames)]
                public async ValueTask<AdminCommandResult> SetNicknameAsync([RequireHierarchy] CachedMember target)
                {
                    await target.ModifyAsync(x => x.Nick = string.Empty);
                    return CommandSuccessLocalized("user_nickname_reset");
                }

                [Command("reset")]
                [RequireBotPermissions(Permission.ManageNicknames)]
                public async ValueTask<AdminCommandResult> SetNicknameAsync()
                {
                    var target = (CachedMember)Context.User;
                    if (Context.Guild.CurrentMember.Hierarchy <= target.Hierarchy)
                        return CommandErrorLocalized("requirehierarchy_self");

                    await target.ModifyAsync(x => x.Nick = string.Empty);
                    return CommandSuccessLocalized("user_nickname_reset");
                }
            }

            [Command("search"), RunMode(RunMode.Parallel)]
            [RequireUserPermissions(Permission.ManageMessages, false, Group = "user")]
            [RequireUserPermissions(Permission.ManageMessages, Group = "user")]
            public async ValueTask<AdminCommandResult> SearchUsersAsync(
                [Remainder, MustBe(StringLength.ShorterThan, 33)] string input)
            {
                // input = input.ToLower();
                var matches = new List<ValueTuple<int, CachedMember>>();
                foreach (var user in Context.Guild.Members.Values.Where(x => !string.IsNullOrWhiteSpace(x.Name)))
                {
                    var nickDistance = int.MaxValue;
                    if (!string.IsNullOrWhiteSpace(user.Nick))
                    {
                        nickDistance = input.GetLevenshteinDistanceTo(user.Nick.ToLower());
                        if (user.Nick.Contains(input, StringComparison.OrdinalIgnoreCase))
                            nickDistance -= 6;
                    }
                    
                    var nameDistance = input.GetLevenshteinDistanceTo(user.Name.ToLower());
                    if (user.Name.Contains(input, StringComparison.OrdinalIgnoreCase))
                        nameDistance -= 8; // higher weighting for usernames

                    var distance = Math.Min(nameDistance, nickDistance);
                    if (distance <= 2)
                        matches.Add((distance, user));
                }

                if (matches.Count == 0)
                    return CommandErrorLocalized("user_search_no_results", args: input);

                matches = matches.OrderBy(x => x.Item1).ToList();

                var pages = DefaultPaginator.GeneratePages(matches, 1024, x => FormatUser(x.Item2),
                    builder: new LocalEmbedBuilder().WithSuccessColor().WithTitle(Localize("user_search_results", input)));

                if (pages.Count > 1)
                {
                    await Pagination.SendPaginatorAsync(Context.Channel, new DefaultPaginator(pages, 0), pages[0]);
                    return CommandSuccess();
                }

                return CommandSuccess(embed: pages[0].Embed);
            }

            [Command("searchregex"), RunMode(RunMode.Parallel)]
            [RequireUserPermissions(Permission.ManageMessages, false, Group = "user")]
            [RequireUserPermissions(Permission.ManageMessages, Group = "user")]
            public async ValueTask<AdminCommandResult> RegexSearchUsersAsync([Remainder] Regex regex)
            {
                var matches = new List<CachedMember>();
                var delay = Task.Delay(TimeSpan.FromSeconds(5));
                var task = Task.Run(() =>
                {
                    matches = Context.Guild.Members.Values.Where(MatchesRegex)
                        .OrderByDescending(x => x.JoinedAt)
                        .ToList();
                });

                using var _ = Context.Channel.Typing();
                var timeoutTask = await Task.WhenAny(delay, task);
                if (timeoutTask == delay)
                    return CommandErrorLocalized("user_searchregex_timeout");
                
                if (matches.Count == 0)
                    return CommandErrorLocalized("user_searchregex_no_results", args: regex.ToString());

                var pages = DefaultPaginator.GeneratePages(matches, 1024, FormatUser, builder: new LocalEmbedBuilder()
                    .WithSuccessColor().WithTitle(Localize("user_searchregex_results")));

                if (pages.Count > 1)
                {
                    await Pagination.SendPaginatorAsync(Context.Channel, new DefaultPaginator(pages, 0), pages[0]);
                    return CommandSuccess();
                }

                return CommandSuccess(embed: pages[0].Embed);

                bool MatchesRegex(CachedMember target)
                {
                    if (string.IsNullOrWhiteSpace(target.Name)) return false;

                    if (string.IsNullOrWhiteSpace(target.Nick))
                        return regex.IsMatch(target.Name);

                    return regex.IsMatch(target.Nick) || regex.IsMatch(target.Name);
                }
            }

            private string FormatUser(CachedMember target)
            {
                return string.IsNullOrWhiteSpace(target.Nick)
                    ? target.Format()
                    : Localize("user_search_format", Markdown.Bold(target.Tag.Sanitize()),
                        Markdown.Bold(target.Nick.Sanitize()),
                        Markdown.Code(target.Id.ToString()));
            }
        }

        [Command("xp"), RunMode(RunMode.Parallel)]
        public async ValueTask<AdminCommandResult> GetUserXpAsync([Remainder] CachedMember target = null)
        {
            using var _ = Context.Channel.Typing();
            var image = await Levels.CreateXpImageAsync(Context, target ?? Context.User);

            return CommandSuccess(attachment: new LocalAttachment(image, "xp.png"));
        }

        [Command("language")]
        public async ValueTask<AdminCommandResult> GetLanguageAsync()
        {
            var user = await Context.Database.GetOrCreateGlobalUserAsync(Context.User.Id);
            return CommandSuccessLocalized("user_language", args:
                $"{Markdown.Bold(user.Language.NativeName)} ({user.Language.EnglishName}, `{user.Language.CultureCode}`)");
        }

        [Command("language")]
        public async ValueTask<AdminCommandResult> SetLanguageAsync([Remainder] LocalizedLanguage newLanguage)
        {
            var user = await Context.Database.GetOrCreateGlobalUserAsync(Context.User.Id);
            user.Language = newLanguage;
            Context.Database.GlobalUsers.Update(user);
            await Context.Database.SaveChangesAsync();
            Context.Language = newLanguage;
            CommandHandler.UpdateLanguage(Context.User.Id, newLanguage);

            return CommandSuccessLocalized("user_language_set", args:
                $"{Markdown.Bold(user.Language.NativeName)} ({user.Language.EnglishName}, `{user.Language.CultureCode}`)");
        }

        [Command("languages")]
        [IgnoresExtraArguments]
        public AdminCommandResult GetLanguages()
            => CommandSuccess(new StringBuilder()
                .AppendNewline(Localize("available_languages"))
                .AppendJoin('\n',
                    Localization.Languages.Select(
                        x => Markdown.Code($"{x.NativeName} ({x.EnglishName}, {x.CultureCode})"))).ToString());
    }
}