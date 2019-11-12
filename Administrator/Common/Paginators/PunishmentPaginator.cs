using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Administrator.Commands;
using Administrator.Database;
using Administrator.Extensions;
using Administrator.Services;
using Discord;
using Discord.WebSocket;

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
            : base(new[] { EmoteTools.Left, EmoteTools.Right })
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
                await Message.RemoveAllReactionsAsync();
            }
        }

        public override async ValueTask<Page> GetPageAsync(IEmote emote, IUser user)
        {
            if (!_isPrivateMessage)
            {
                await Message.RemoveReactionAsync(emote, user);
            }

            if (emote.Equals(EmoteTools.Left) && _currentPage > 0)
            {
                _tokenSource.CancelAfter(TimeSpan.FromSeconds(30));
                _currentPage--;
                return await BuildPageAsync();
            }

            if (emote.Equals(EmoteTools.Right) && _currentPage < _pages.Count - 1)
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

            var builder = new EmbedBuilder()
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
                    .AppendLine(_context.Localize("punishment_target", target?.Format(false) ?? "???"))
                    .AppendLine(_context.Localize("punishment_moderator", moderator?.Format(false) ?? "???"));

                if (punishment is Mute mute && mute.ChannelId.HasValue)
                {
                    sb.AppendLine(_context.Localize("punishment_mute_channel") + ": " +
                                  (_context.Guild.GetTextChannel(mute.ChannelId.Value)?.Mention ?? "???"));
                }

                sb.AppendLine(_context.Localize("title_reason") + ": " + punishment.Reason.TrimTo(512))
                    .AppendLine(_context.Localize("punishment_timestamp",
                        punishment.CreatedAt.ToString("g", _context.Language.Culture)));

                if (punishment is RevocablePunishment revocable)
                {
                    if (revocable.IsAppealable)
                    {
                        sb.AppendLine(_context.Localize("punishment_appealed") + ' ' +
                                      (revocable.AppealedAt.HasValue
                                          ? $"✅ {revocable.AppealedAt.Value.ToString("g", _context.Language.Culture)} - {revocable.AppealReason.TrimTo(950)}"
                                          : "❌"));
                    }

                    var revoker = revocable.RevokedAt.HasValue
                        ? await _context.Client.GetOrDownloadUserAsync(revocable.RevokerId)
                        : default;

                    sb.AppendLine(_context.Localize("punishment_revoked") + ' ' + (revocable.RevokedAt.HasValue
                                      ? "✅ " + revocable.RevokedAt.Value.ToString("g", _context.Language.Culture) +
                                        $" - {Format.Bold(revoker?.ToString() ?? "???")} - " +
                                        (revocable.RevocationReason?.TrimTo(920) ??
                                         _context.Localize("punishment_noreason"))
                                      : "❌"));
                }

                builder.AddField(name, sb.ToString());
            }

            return builder.Build();
        }

        /*
        private static readonly Emoji Left = new Emoji("⬅");
        private static readonly Emoji Right = new Emoji("➡");

        private readonly AdminCommandContext _context;
        private readonly IDictionary<ulong, IUser> _cachedUsers;
        private readonly List<List<Punishment>> _pages;
        private readonly Timer _timer;
        private readonly ulong _targetId;
        private readonly PunishmentListType _type;
        private readonly CancellationTokenSource _expiryToken;
        private int _currentPage;

        public PunishmentPaginator(IUserMessage message, List<List<Punishment>> pages, int currentPage, ulong targetId, PunishmentListType type, PaginationService service) 
            : base(message, new IEmote[] {Left, Right}, service)
        {
            _cachedUsers = new Dictionary<ulong, IUser>();
            _pages = pages;
            _currentPage = currentPage;
            _timer = new Timer(Expire, this, TimeSpan.FromSeconds(30), TimeSpan.FromMilliseconds(-1));
            _targetId = targetId;
            _type = type;
            _expiryToken = new CancellationTokenSource();
        }

        public override async ValueTask<Page> GetPageAsync(IUser user, IEmote emote)
        {
            if (emote.Equals(Left) && _currentPage > 0)
            {
                _timer.Change(TimeSpan.FromSeconds(30), TimeSpan.FromMilliseconds(-1));
                _currentPage--;
                _ = Message.RemoveReactionAsync(emote, user);
                return await BuildPageAsync();
            }

            if (emote.Equals(Right) && _currentPage < _pages.Count - 1)
            {
                _timer.Change(TimeSpan.FromSeconds(30), TimeSpan.FromMilliseconds(-1));
                _currentPage++;
                _ = Message.RemoveReactionAsync(emote, user);
                return await BuildPageAsync();
            }

            _ = Message.RemoveReactionAsync(emote, user);
            return null;
        }

        public override Task CloseAsync()
        {
            _ = Message?.RemoveAllReactionsAsync();
            return Task.CompletedTask;
        }

        public override async ValueTask DisposeAsync()
        {
            await _timer.DisposeAsync();
            await base.DisposeAsync();
        }

        public Task WaitForExpiryAsync()
            => Task.Delay(-1, _expiryToken.Token);

        private void Expire(object _)
        {
            _expiryToken.Cancel();
            _ = CloseAsync();
        }

        public async ValueTask<Page> BuildPageAsync()
        {
            var titleName = _context.Guild.Name;
            if (_type == PunishmentListType.User)
            {
                var target = await _context.Client.GetOrDownloadUserAsync(_targetId);
                titleName = target?.Format() ?? "???";
            }

            var builder = new EmbedBuilder()
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
                    .AppendLine(_context.Localize("punishment_target", target?.Format(false) ?? "???"))
                    .AppendLine(_context.Localize("punishment_moderator", moderator?.Format(false) ?? "???"));

                if (punishment is Mute mute && mute.ChannelId.HasValue)
                {
                    sb.AppendLine(_context.Localize("punishment_mute_channel") + ": " +
                                  (_context.Guild.GetTextChannel(mute.ChannelId.Value)?.Mention ?? "???"));
                }

                sb.AppendLine(_context.Localize("title_reason") + ": " + punishment.Reason.TrimTo(512))
                    .AppendLine(_context.Localize("punishment_timestamp",
                        punishment.CreatedAt.ToString("g", _context.Language.Culture)));

                if (punishment is RevocablePunishment revocable)
                {
                    if (revocable.IsAppealable)
                    {
                        sb.AppendLine(_context.Localize("punishment_appealed") + ' ' +
                                      (revocable.AppealedAt.HasValue
                                          ? $"✅ {revocable.AppealedAt.Value.ToString("g", _context.Language.Culture)} - {revocable.AppealReason.TrimTo(950)}"
                                          : "❌"));
                    }

                    var revoker = revocable.RevokedAt.HasValue
                        ? await _context.Client.GetOrDownloadUserAsync(revocable.RevokerId)
                        : default;

                    sb.AppendLine(_context.Localize("punishment_revoked") + ' ' + (revocable.RevokedAt.HasValue
                                      ? "✅ " + revocable.RevokedAt.Value.ToString("g", _context.Language.Culture) +
                                        $" - {Format.Bold(revoker?.ToString() ?? "???")} - " +
                                        (revocable.RevocationReason?.TrimTo(920) ??
                                         _context.Localize("punishment_noreason"))
                                      : "❌"));
                }

                builder.AddField(name, sb.ToString());
            }

            return builder.Build();
        }
        */
    }
}