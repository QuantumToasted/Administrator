using Administrator.Database;
using Microsoft.Extensions.DependencyInjection;

namespace Administrator.Bot;

public static class ServiceProviderExtensions
{
    public static AsyncServiceScope CreateAsyncScopeWithDatabase(this IServiceProvider services, out AdminDbContext db)
    {
        var scope = services.CreateAsyncScope();
        db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
        return scope;
    }
}