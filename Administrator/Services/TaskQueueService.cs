using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Administrator.Common;

namespace Administrator.Services
{
    public sealed class TaskQueueService : IService
    {
        private readonly ConcurrentQueue<Func<Task>> _queue;
        private readonly SemaphoreSlim _semaphore;

        private event Func<Task> CollectionChanged;

        public TaskQueueService()
        {
            _queue = new ConcurrentQueue<Func<Task>>();
            _semaphore = new SemaphoreSlim(1, 1);
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
                    Log.Error(ex.InnerException);
                }
            }

            _semaphore.Release();
        }

        public Task InitializeAsync()
        {
            CollectionChanged += () 
                => Task.Run(EmptyQueueAsync);

            Log.Verbose("Initialized.");
            return Task.CompletedTask;
        }
    }
}