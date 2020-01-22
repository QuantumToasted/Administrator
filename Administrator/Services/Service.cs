using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Administrator.Services
{
    public abstract class Service
    {
        private protected readonly IServiceProvider _provider;

        protected Service(IServiceProvider provider)
        {
            _provider = provider;
        }

        public virtual Task InitializeAsync()
            => _provider.GetRequiredService<LoggingService>()
                .LogInfoAsync("Initialized.", GetType().Name.Replace("Service", string.Empty));
    }
}
