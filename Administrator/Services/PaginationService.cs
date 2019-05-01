using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Administrator.Common;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace Administrator.Services
{
    public sealed class PaginationService : IService
    {
        private readonly ICollection<Paginator> _paginators;
        private readonly LoggingService _logging;

        public PaginationService(LoggingService logging)
        {
            _paginators = new List<Paginator>();
            _logging = logging;
        }

        public Task<RestUserMessage> SendPaginatorAsync(ISocketMessageChannel channel, Page page)
            => channel.SendMessageAsync(page.Text, embed: page.Embed);

        public void AddPaginator(Paginator paginator)
        {
            _paginators.Add(paginator);
            _ = paginator.Message?.AddReactionsAsync(paginator.Emotes);
        }

        public void RemovePaginator(Paginator paginator)
        {
            _paginators.Remove(paginator);
            _ = paginator.Message?.RemoveAllReactionsAsync();
        }

        public async Task ModifyPaginatorsAsync(Cacheable<IUserMessage, ulong> cacheable, ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            if (!reaction.User.IsSpecified || reaction.User.Value.IsBot)
                return;

            if (!(_paginators.FirstOrDefault(x => x.Message.Id == cacheable.Id) is Paginator paginator))
                return;

            var nextPage = await paginator.GetPageAsync(reaction.User.Value, reaction.Emote);
            if (nextPage is null) return;
            await paginator.Message.ModifyAsync(x =>
            {
                x.Content = nextPage.Text;
                if (nextPage.Embed is null) return;
                x.Embed = nextPage.Embed;
            });
        }

        Task IService.InitializeAsync()
            => _logging.LogInfoAsync("Initialized.", "Pagination");
    }
}