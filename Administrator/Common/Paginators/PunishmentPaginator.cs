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

namespace Administrator.Common
{
    public sealed class PunishmentPaginator : Paginator
        //where TPunishment : Punishment
    {
        private static readonly Emoji Left = new Emoji("⬅");
        private static readonly Emoji Right = new Emoji("➡");

        private readonly AdminCommandContext _context;
        private readonly IDictionary<ulong, IUser> _cachedUsers;
        private readonly List<List<Punishment>> _pages;
        private readonly Timer _timer;
        private readonly ulong _targetId;
        private readonly PunishmentListType _type;
        private int _currentPage;

        public PunishmentPaginator(IUserMessage message, List<List<Punishment>> pages, int currentPage,
            AdminCommandContext context, ulong targetId, PunishmentListType type, PaginationService service = null) 
            : base(message, new IEmote[] {Left, Right}, service)
        {
            _cachedUsers = new Dictionary<ulong, IUser>();
            _pages = pages;
            _currentPage = currentPage;
            _timer = new Timer(Expire, this, TimeSpan.FromSeconds(30), TimeSpan.FromMilliseconds(-1));
            _context = context;
            _targetId = targetId;
            _type = type;
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
            => Task.CompletedTask;

        public override void Dispose()
        {
            base.Dispose();
            _timer.Dispose();
        }

        private void Expire(object state)
            => Dispose();

        public async ValueTask<Page> BuildPageAsync()
        {
            var titleName = _context.Guild.Name;
            if (_type == PunishmentListType.User)
            {
                var target = _context.Client.GetUser(_targetId)
                             ?? await _context.Client.Rest.GetUserAsync(_targetId) as IUser;
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
                    : _cachedUsers[punishment.TargetId] = _context.Client.GetUser(punishment.TargetId) ??
                                                          await _context.Client.Rest.GetUserAsync(punishment
                                                              .TargetId) as IUser;

                var moderator = _cachedUsers.TryGetValue(punishment.ModeratorId, out cached)
                    ? cached
                    : _cachedUsers[punishment.ModeratorId] = _context.Client.GetUser(punishment.ModeratorId) ??
                                                             await _context.Client.Rest.GetUserAsync(punishment
                                                                 .ModeratorId) as IUser;

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
                        ? _context.Client.GetUser(revocable.RevokerId) ??
                          await _context.Client.Rest.GetUserAsync(revocable.RevokerId) as IUser
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
    }
}