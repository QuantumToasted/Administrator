using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Administrator.Common;
using Disqord;
using Disqord.Events;

namespace Administrator.Services
{
    public sealed class PaginationService : IService, IHandler<ReactionAddedEventArgs>
    {
        private readonly ICollection<Paginator> _paginators;
        private readonly LoggingService _logging;

        public PaginationService(LoggingService logging)
        {
            _paginators = new List<Paginator>();
            _logging = logging;
        }

        public async Task SendPaginatorAsync(ICachedMessageChannel channel, Paginator paginator, Page firstPage)
        {
            _paginators.Add(paginator
                .WithMessage(await channel.SendMessageAsync(firstPage.Text, embed: firstPage.Embed), channel)
                .WithService(this));

            await paginator.AddReactionAsync();
        }

        public bool RemovePaginator(Paginator paginator)
            => _paginators.Remove(paginator);

        public async Task HandleAsync(ReactionAddedEventArgs args)
        {
            if (!args.Reaction.HasValue) return;

            if (!(_paginators.FirstOrDefault(x => x.Message.Id == args.Message.Id) is { } paginator))
                return;

            var nextPage = await paginator.GetPageAsync(args.Reaction.Value.Emoji, args.User.Id);
            if (nextPage is null) return;
            await paginator.Message.ModifyAsync(x =>
            {
                x.Content = nextPage.Text;
                if (nextPage.Embed is null) return;
                x.Embed = nextPage.Embed;
            });
        }
        /*
        private readonly ICollection<Paginator> _paginators;
        private readonly LoggingService _logging;

        public PaginationService(LoggingService logging)
        {
            _paginators = new List<Paginator>();
            _logging = logging;
        }

        public Task<RestUserMessage> SendPaginatorAsync(ICachedMessageChannel channel, Page page)
            => channel.SendMessageAsync(page.Text, embed: page.Embed);

        public void AddPaginator(Paginator paginator)
        {
            _paginators.Add(paginator);
            _ = paginator.Message.AddReactionsAsync(paginator.emojis);
        }

        public void RemovePaginator(Paginator paginator)
        {
            _paginators.Remove(paginator);
            _ = paginator.Message.RemoveAllReactionsAsync();
        }

        public async Task ModifyPaginatorsAsync(Cacheable<IUserMessage, ulong> cacheable, ICachedMessageChannel channel,
            SocketReaction reaction)
        {
            if (!reaction.User.IsSpecified || reaction.User.Value.IsBot)
                return;

            if (!(_paginators.FirstOrDefault(x => x.Message.Id == cacheable.Id) is Paginator paginator))
                return;

            var nextPage = await paginator.GetPageAsync(reaction.User.Value, reaction.emoji);
            if (nextPage is null) return;
            await paginator.Message.ModifyAsync(x =>
            {
                x.Content = nextPage.Text;
                if (nextPage.Embed is null) return;
                x.Embed = nextPage.Embed;
            });
        }
        */

        Task IService.InitializeAsync()
            => _logging.LogInfoAsync("Initialized.", "Pagination");
    }
}