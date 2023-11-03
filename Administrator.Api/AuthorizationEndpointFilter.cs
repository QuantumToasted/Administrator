using Microsoft.AspNetCore.Http;

namespace Administrator.Api;

public sealed class AuthorizationEndpointFilter : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        throw new NotImplementedException();
    }
}