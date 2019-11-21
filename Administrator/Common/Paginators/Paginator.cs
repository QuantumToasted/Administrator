using System;
using System.Threading.Tasks;
using Administrator.Services;
using Disqord;

namespace Administrator.Common
{
    public abstract class Paginator : IAsyncDisposable
    {
        private protected bool _isPrivateMessage;
        private protected PaginationService _service;

        protected Paginator(IEmoji[] emojis)
        {
            Emojis = emojis;
        }

        public Paginator WithMessage(IUserMessage message, IMessageChannel channel)
        {
            Message = message;
            _isPrivateMessage = channel is IPrivateChannel;
            return this;
        }

        public Paginator WithService(PaginationService service)
        {
            _service = service;
            return this;
        }

        public async Task AddReactionAsync()
        {
            for (var i = 0; i < Emojis.Length; i++)
            {
                await Message.AddReactionAsync(Emojis[i]);
            }
        }

        public IEmoji[] Emojis { get; }

        public IUserMessage Message { get; private set; }

        public abstract ValueTask<Page> GetPageAsync(IEmoji emoji, Snowflake userId);

        public abstract ValueTask DisposeAsync();

        public override bool Equals(object obj)
            => (obj as Paginator)?.Message.Id == Message.Id;

        /*
        private readonly PaginationService _service;

        protected Paginator(IUserMessage message, IEmoji[] emojis, PaginationService service)
        {
            Message = message;
            emojis = emojis;
            (_service = service).AddPaginator(this);
        }

        public IUserMessage Message { get; }

        public IEmoji[] emojis { get; }

        public abstract ValueTask<Page> GetPageAsync(IUser user, IEmoji emoji);

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