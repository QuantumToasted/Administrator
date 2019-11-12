using System;
using System.Threading.Tasks;
using Administrator.Services;
using Discord;

namespace Administrator.Common
{
    public abstract class Paginator : IAsyncDisposable
    {
        private protected bool _isPrivateMessage;
        private protected PaginationService _service;

        protected Paginator(IEmote[] emotes)
        {
            Emotes = emotes;
        }

        public Paginator WithMessage(IUserMessage message)
        {
            Message = message;
            _isPrivateMessage = Message.Channel is IPrivateChannel;
            return this;
        }

        public Paginator WithService(PaginationService service)
        {
            _service = service;
            return this;
        }

        public async Task AddReactionAsync()
        {
            for (var i = 0; i < Emotes.Length; i++)
            {
                await Message.AddReactionAsync(Emotes[i]);
            }
        }

        public IEmote[] Emotes { get; }

        public IUserMessage Message { get; private set; }

        public abstract ValueTask<Page> GetPageAsync(IEmote emote, IUser user);

        public abstract ValueTask DisposeAsync();

        public override bool Equals(object obj)
            => (obj as Paginator)?.Message.Id == Message.Id;

        /*
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
        */
    }
}