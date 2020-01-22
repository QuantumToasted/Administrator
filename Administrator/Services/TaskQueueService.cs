using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Administrator.Services
{
    public sealed class TaskQueueService : Service
    {
        private readonly ConcurrentQueue<Func<Task>> _queue;
        private readonly SemaphoreSlim _semaphore;
        private readonly LoggingService _logging;

        private event Func<Task> CollectionChanged;

        public TaskQueueService(IServiceProvider provider)
            : base(provider)
        {
            _queue = new ConcurrentQueue<Func<Task>>();
            _semaphore = new SemaphoreSlim(1, 1);
            _logging = _provider.GetRequiredService<LoggingService>();
        }

        public Task Enqueue(Func<Task> func)
        {
            _queue.Enqueue(func);
            return CollectionChanged?.Invoke() ?? Task.CompletedTask;
        }

        public override Task InitializeAsync()
        {
            CollectionChanged += () =>
            {
                _ = Task.Run(EmptyQueueAsync);
                return Task.CompletedTask;
            };

            return base.InitializeAsync();
        }

        private async Task EmptyQueueAsync()
        {
            await _semaphore.WaitAsync();

            while (_queue.TryDequeue(out var func))
            {
                try
                {
                    await func();
                }
                catch (Exception ex)
                {
                    await _logging.LogErrorAsync(ex/*.InnerException*/, "TaskQueue");
                }
            }

            _semaphore.Release();
        }
    }
}