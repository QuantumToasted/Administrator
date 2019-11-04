using System;
using System.Threading.Tasks;
using Administrator.Services;
using Discord;

namespace Administrator.Common
{
    public abstract class Paginator : IAsyncDisposable
    {
        private readonly PaginationService _service;

        protected Paginator(IUserMessage message, IEmote[] emotes, PaginationService service)
        {
            Message = message;
            Emotes = emotes;
            (_service = service).AddPaginator(this);
        }

        public IUserMessage Message { get; }

        public IEmote[] Emotes { get; }

        public abstract ValueTask<Page> GetPageAsync(IUser user, IEmote emote);

        public abstract Task CloseAsync();

        public override bool Equals(object obj)
        {
            return obj is Paginator other && other.Message.Id == Message.Id;
        }

        public override int GetHashCode()
        {
            return Message != null ? Message.GetHashCode() : 0;
        }

        public virtual ValueTask DisposeAsync()
        {
            _service.RemovePaginator(this);
            return new ValueTask(CloseAsync());
        }
    }
}