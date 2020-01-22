using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Administrator.Commands;
using Administrator.Database;
using Administrator.Extensions;
using Disqord;

namespace Administrator.Common
{
    public sealed class PunishmentPaginator : Paginator
    {
        private readonly PunishmentListType _type;
        private readonly IDictionary<ulong, IUser> _cachedUsers;
        private readonly ulong _targetId;
        private readonly AdminCommandContext _context;
        private readonly List<List<Punishment>> _pages;
        private readonly CancellationTokenSource _tokenSource;
        private int _currentPage;

        public PunishmentPaginator(List<List<Punishment>> pages, int currentPage, ulong targetId, 
            PunishmentListType type, AdminCommandContext context)
            : base(new[] { EmojiTools.Left, EmojiTools.Right })
        {
            _pages = pages;
            _currentPage = currentPage;
            _targetId = targetId;
            _type = type;
            _context = context;
            _tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            _cachedUsers = new Dictionary<ulong, IUser>();

            Task.Delay(-1, _tokenSource.Token).ContinueWith(_ => DisposeAsync());
        }

        public override async ValueTask DisposeAsync()
        {
            _tokenSource.Dispose();
            _service.RemovePaginator(this);

            if (!_isPrivateMessage)
            {
                await Message.ClearReactionsAsync();
            }
        }

        public override async ValueTask<Page> GetPageAsync(IEmoji emoji, Snowflake userId)
        {
            if (!_isPrivateMessage)
            {
                await Message.RemoveMemberReactionAsync(userId, emoji);
            }

            if (emoji.Equals(EmojiTools.Left) && _currentPage > 0)
            {
                _tokenSource.CancelAfter(TimeSpan.FromSeconds(30));
                _currentPage--;
                return await BuildPageAsync();
            }

            if (emoji.Equals(EmojiTools.Right) && _currentPage < _pages.Count - 1)
            {
                _tokenSource.CancelAfter(TimeSpan.FromSeconds(30));
                _currentPage++;
                return await BuildPageAsync();
            }      

            return null;
        }

        public async ValueTask<Page> BuildPageAsync()
        {
            var titleName = _context.Guild.Name.Sanitize();
            if (_type == PunishmentListType.User)
            {
                var target = await _context.Client.GetOrDownloadUserAsync(_targetId);
                titleName = target?.Format() ?? "???";
            }

            var builder = new LocalEmbedBuilder()
                .WithSuccessColor()
                .WithTitle(_context.Localize("punishment_list_title", titleName))
                .WithFooter($"{_currentPage + 1}/{_pages.Count}");

            foreach (var punishment in _pages[_currentPage])
            {
                var target = _cachedUsers.TryGetValue(punishment.TargetId, out var cached)
                    ? cached
                    : _cachedUsers[punishment.TargetId] = await _context.Client
                        .GetOrDownloadUserAsync(punishment.TargetId);

                var moderator = _cachedUsers.TryGetValue(punishment.ModeratorId, out cached)
                    ? cached
                    : _cachedUsers[punishment.ModeratorId] = await _context.Client
                        .GetOrDownloadUserAsync(punishment.ModeratorId);

                var name = _context.Localize($"punishment_{punishment.GetType().Name.ToLower()}") +
                           $" - {_context.Localize("punishment_case", punishment.Id)}";

                var sb = new StringBuilder()
                    .AppendNewline(_context.Localize("punishment_target", target?.Format(false) ?? "???"))
                    .AppendNewline(_context.Localize("punishment_moderator", moderator?.Format(false) ?? "???"));

                if (punishment is Mute mute && mute.ChannelId.HasValue)
                {
                    sb.AppendNewline(_context.Localize("punishment_mute_channel") + ": " +
                                  (_context.Guild.GetTextChannel(mute.ChannelId.Value)?.Mention ?? "???"));
                }

                sb.AppendNewline(_context.Localize("title_reason") + ": " + punishment.Reason.TrimTo(512))
                    .AppendNewline(_context.Localize("punishment_timestamp",
                        punishment.CreatedAt.ToString("g", _context.Language.Culture)));

                if (punishment is RevocablePunishment revocable)
                {
                    if (revocable.IsAppealable)
                    {
                        sb.AppendNewline(_context.Localize("punishment_appealed") + ' ' +
                                      (revocable.AppealedAt.HasValue
                                          ? $"✅ {revocable.AppealedAt.Value.ToString("g", _context.Language.Culture)} - {revocable.AppealReason.TrimTo(950)}"
                                          : "❌"));
                    }

                    var revoker = revocable.RevokedAt.HasValue
                        ? await _context.Client.GetOrDownloadUserAsync(revocable.RevokerId)
                        : default;

                    sb.AppendNewline(_context.Localize("punishment_revoked") + ' ' + (revocable.RevokedAt.HasValue
                                      ? "✅ " + revocable.RevokedAt.Value.ToString("g", _context.Language.Culture) +
                                        $" - {Markdown.Bold(revoker?.Tag ?? "???")} - " +
                                        (revocable.RevocationReason?.TrimTo(920) ??
                                         _context.Localize("punishment_noreason"))
                                      : "❌"));
                }

                builder.AddField(name, sb.ToString());
            }

            return builder.Build();
        }
    }
}