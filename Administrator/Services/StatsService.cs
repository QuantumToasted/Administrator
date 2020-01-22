using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Extensions;
using Disqord;
using Disqord.Events;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;

namespace Administrator.Services
{
    public sealed class StatsService : Service,
        IHandler<MessageReceivedEventArgs>,
        IHandler<CommandExecutedEventArgs>
    {
        private readonly DiscordClient _client;
        private readonly LoggingService _logging;
        private readonly DateTimeOffset _startTime;

        public StatsService(IServiceProvider provider)
            : base(provider)
        {
            _client = _provider.GetRequiredService<DiscordClient>();
            _logging = _provider.GetRequiredService<LoggingService>();
            _startTime = DateTimeOffset.UtcNow;
            CustomAssemblies = new List<Assembly>();
        }

        public List<Assembly> CustomAssemblies { get; private set; }

        public DateTimeOffset BuildDate { get; private set; }

        public int MessagesReceived { get; private set; }

        public int CommandsExecuted { get; private set; }

        public int TotalGuilds => _client.Guilds.Count;

        public int TotalTextChannels => _client.Guilds.Values.Sum(x => x.TextChannels.Count);

        public int TotalVoiceChannels => _client.Guilds.Values.Sum(x => x.VoiceChannels.Count);

        public int TotalMembers => _client.Guilds.Values.Sum(x => x.MemberCount);

        public long MemoryUsage // in bytes
        {
            get
            {
                using var process = Process.GetCurrentProcess();
                return Math.Max(process.PrivateMemorySize64, GC.GetTotalMemory(true));
            }
        }

        public TimeSpan Uptime => DateTimeOffset.UtcNow - _startTime;

        public Task HandleAsync(MessageReceivedEventArgs args)
        {
            MessagesReceived++;
            return Task.CompletedTask;
        }

        public Task HandleAsync(CommandExecutedEventArgs args)
        {
            CommandsExecuted++;
            return Task.CompletedTask;
        }

        public override Task InitializeAsync()
        {
            BuildDate = Assembly.GetExecutingAssembly().GetCustomAttribute<BuildDateAttribute>()?.Date ?? default;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().DistinctBy(x => x.FullName))
            {
                if (!assembly.FullName.Contains("Administrator") &&
                    !assembly.FullName.Contains("System") && 
                    !assembly.FullName.Contains("Microsoft") && 
                    !assembly.FullName.Contains("netstandard") &&
                    !assembly.FullName.Contains("DynamicMethods") &&
                    !assembly.FullName.Contains("Linq"))
                    CustomAssemblies.Add(assembly);
            }

            return base.InitializeAsync();
        }
    }
}