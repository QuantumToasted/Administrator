using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Administrator.Services
{
    public sealed class TaskQueueService : IService
    {
        private readonly ConcurrentQueue<Func<Task>> _queue;
        private readonly SemaphoreSlim _semaphore;
        private readonly LoggingService _logging;

        private event Func<Task> CollectionChanged;

        public TaskQueueService(LoggingService logging)
        {
            _queue = new ConcurrentQueue<Func<Task>>();
            _semaphore = new SemaphoreSlim(1, 1);
            _logging = logging;
        }

        public Task Enqueue(Func<Task> func)
        {
            _queue.Enqueue(func);
            return CollectionChanged?.Invoke() ?? Task.CompletedTask;
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
                    await _logging.LogErrorAsync(ex.InnerException, "TaskQueue");
                }
            }

            _semaphore.Release();
        }

        Task IService.InitializeAsync()
        {
            CollectionChanged += ()
                =>
            {
                _ = Task.Run(EmptyQueueAsync);
                return Task.CompletedTask;
            };

            return _logging.LogInfoAsync("Initialized.", "TaskQueue");
        }
    }
}