using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Administrator.Services
{
    public sealed class LoggingService : IService
    {
        private const int PAD_LENGTH = 24;
        private readonly SemaphoreSlim _semaphore;

        public LoggingService()
        {
            _semaphore = new SemaphoreSlim(1, 1);
        }
        
        public bool Verbose { get; set; }
        
        public Task LogCriticalAsync(object value, [CallerFilePath] string source = "",
            ConsoleColor textColor = ConsoleColor.Gray)
            => LogMessageAsync(value, source, "CRIT", ConsoleColor.Red, textColor);

        public Task LogErrorAsync(object value, [CallerFilePath] string source = "",
            ConsoleColor textColor = ConsoleColor.Gray)
            => LogMessageAsync(value, source, "ERRO", ConsoleColor.Yellow, textColor);

        public Task LogWarningAsync(object value, [CallerFilePath] string source = "",
            ConsoleColor textColor = ConsoleColor.Gray)
            => LogMessageAsync(value, source, "WARN", ConsoleColor.Magenta, textColor);

        public Task LogInfoAsync(object value, [CallerFilePath] string source = "",
            ConsoleColor textColor = ConsoleColor.Gray)
            => LogMessageAsync(value, source, "INFO", ConsoleColor.Green, textColor);

        public Task LogVerboseAsync(object value, [CallerFilePath] string source = "",
            ConsoleColor textColor = ConsoleColor.Gray)
            => Verbose ? LogMessageAsync(value, source, "VRBS", ConsoleColor.White, textColor) : Task.CompletedTask;

        public Task LogDebugAsync(object value, [CallerFilePath] string source = "",
            ConsoleColor textColor = ConsoleColor.Gray)
#if DEBUG
            => LogMessageAsync(value, source, "DBUG", ConsoleColor.DarkGray, textColor);
#else
            => Task.CompletedTask;
#endif

        private async Task LogMessageAsync(object value, string source, string level, ConsoleColor levelColor,
            ConsoleColor textColor)
        {
            await _semaphore.WaitAsync();

            var text = value.ToString();
            if (string.IsNullOrWhiteSpace(text)) return;

            foreach (var message in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                Console.Write('[');
                Console.ForegroundColor = levelColor;
                Console.Write(level);
                Console.ResetColor();

                try
                {
                    source = Path.GetFileNameWithoutExtension(new Uri(source).AbsolutePath);
                }
                catch
                {
                    // wasn't a file path, just use the caller name directly
                }

                var spaces = PAD_LENGTH - source.Length;
                var padLeft = spaces / 2 + source.Length;
                Console.Write($"|{source.PadLeft(padLeft).PadRight(PAD_LENGTH)}] ");

                Console.ForegroundColor = textColor;
                Console.WriteLine(message);
            }
            
            _semaphore.Release();
        }

        Task IService.InitializeAsync()
            => Task.CompletedTask;
    }
}