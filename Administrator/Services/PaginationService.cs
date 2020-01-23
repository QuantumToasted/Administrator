using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Administrator.Common;
using Disqord;
using Disqord.Events;

namespace Administrator.Services
{
    public sealed class PaginationService : Service,
        IHandler<ReactionAddedEventArgs>
    {
        private readonly ICollection<Paginator> _paginators;

        public PaginationService(IServiceProvider provider)
            : base(provider)
        {
            _paginators = new List<Paginator>();
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
            if (!(_paginators.FirstOrDefault(x => x.Message.Id == args.Message.Id) is { } paginator))
                return;

            var user = args.User.HasValue
                ? args.User.Value
                : await args.User.Downloadable.DownloadAsync() as IUser;

            if (user.IsBot) return;

            var nextPage = await paginator.GetPageAsync(args.Emoji, args.User.Id);
            if (nextPage is null) return;
            await paginator.Message.ModifyAsync(x =>
            {
                x.Content = nextPage.Text;
                if (nextPage.Embed is null) return;
                x.Embed = nextPage.Embed;
            });
        }
    }
}